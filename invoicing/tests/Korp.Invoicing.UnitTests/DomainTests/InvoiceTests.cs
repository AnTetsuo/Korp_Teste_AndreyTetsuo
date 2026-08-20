using Domain.Common;
using Domain.Invoices;
using Domain.Invoices.Enums;
using Domain.Invoices.Items;
using Shouldly;

namespace UnitTests.DomainTests;

public class InvoiceTests
{
    private static InvoiceItemDto Item(
        Guid? productId = null,
        string productCode = "SKU-000000001",
        string description = "Parafuso sextavado 12mm",
        int quantity = 2) =>
        new(productId ?? Guid.CreateVersion7(), productCode, description, quantity);

    [Fact]
    public void Open_WithValidInput_Succeeds()
    {
        var result = Invoice.Open(1, [Item()]);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Status.ShouldBe(ResultStatus.Ok));
    }

    [Fact]
    public void Open_StartsOpen()
    {
        var invoice = Invoice.Open(7, [Item()]).Value;

        invoice.ShouldSatisfyAllConditions(
            () => invoice.Id.ShouldNotBe(Guid.Empty),
            () => invoice.Number.ShouldBe(7),
            () => invoice.Status.ShouldBe(InvoiceStatus.Open));
    }

    [Fact]
    public void Open_StampsUtcTimestamps()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;

        invoice.ShouldSatisfyAllConditions(
            () => invoice.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc),
            () => invoice.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc),
            () => invoice.UpdatedAt.ShouldBe(invoice.CreatedAt));
    }

    [Fact]
    public void Open_MaterialisesEveryItem()
    {
        var first = Item(quantity: 2);
        var second = Item(productCode: "SKU-2", description: "Porca", quantity: 5);

        var invoice = Invoice.Open(1, [first, second]).Value;

        invoice.Items.Count.ShouldBe(2);
        invoice.Items.ShouldAllBe(item => item.InvoiceId == invoice.Id);
        invoice.Items.ShouldAllBe(item => item.Id != Guid.Empty);
    }

    [Fact]
    public void Open_SnapshotsProductCodeAndDescription()
    {
        var source = Item(productCode: "SKU-9", description: "Arruela");

        var item = Invoice.Open(1, [source]).Value.Items.ShouldHaveSingleItem();

        item.ShouldSatisfyAllConditions(
            () => item.ProductId.ShouldBe(source.ProductId),
            () => item.ProductCode.ShouldBe("SKU-9"),
            () => item.Description.ShouldBe("Arruela"),
            () => item.Quantity.ShouldBe(2));
    }

    [Fact]
    public void Open_TrimsSurroundingWhitespace()
    {
        var item = Invoice.Open(1, [Item(productCode: "  SKU-1  ", description: "  Porca  ")])
            .Value.Items.ShouldHaveSingleItem();

        item.ShouldSatisfyAllConditions(
            () => item.ProductCode.ShouldBe("SKU-1"),
            () => item.Description.ShouldBe("Porca"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Open_WithNonPositiveNumber_IsInvalid(long number)
    {
        var result = Invoice.Open(number, [Item()]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "number" &&
            error.Message == "Invoice number must be positive.");
    }

    [Fact]
    public void Open_WithNoItems_IsInvalid()
    {
        var result = Invoice.Open(1, []);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(error =>
                error.Message == "An invoice must have at least one item."));
    }

    [Fact]
    public void Open_WithEmptyProductId_IsInvalid()
    {
        var result = Invoice.Open(1, [Item(productId: Guid.Empty)]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "items[0].productId" &&
            error.Message == "Product id is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Open_WithNonPositiveQuantity_IsInvalid(int quantity)
    {
        var result = Invoice.Open(1, [Item(quantity: quantity)]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "items[0].quantity" &&
            error.Message == "Quantity must be greater than zero.");
    }

    [Fact]
    public void Open_WithBlankProductCode_IsInvalid()
    {
        var result = Invoice.Open(1, [Item(productCode: "   ")]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "items[0].productCode");
    }

    [Fact]
    public void Open_WithOverlongProductCode_IsInvalid()
    {
        var code = new string('X', InvoiceItem.ProductCodeMaxLength + 1);

        var result = Invoice.Open(1, [Item(productCode: code)]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "items[0].productCode" &&
            error.Message.Contains("at most"));
    }

    [Fact]
    public void Open_WithOverlongDescription_IsInvalid()
    {
        var description = new string('X', InvoiceItem.DescriptionMaxLength + 1);

        var result = Invoice.Open(1, [Item(description: description)]);

        result.ValidationErrors.ShouldContain(error =>
            error.Field == "items[0].description" &&
            error.Message.Contains("at most"));
    }

    [Fact]
    public void Open_WithDuplicateProduct_IsInvalid()
    {
        var productId = Guid.CreateVersion7();

        var result = Invoice.Open(1, [Item(productId: productId), Item(productId: productId)]);

        result.ValidationErrors.ShouldContain(error =>
            error.Message.Contains("appears more than once"));
    }

    [Fact]
    public void Open_ReportsEveryFailingLine_NotJustTheFirst()
    {
        var result = Invoice.Open(1, [Item(quantity: 0), Item(productCode: "")]);

        result.ValidationErrors.ShouldSatisfyAllConditions(
            () => result.ValidationErrors.ShouldContain(e => e.Field == "items[0].quantity"),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "items[1].productCode"));
    }

    [Fact]
    public void BeginPrinting_FromOpen_MovesToProcessing()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;

        var result = invoice.BeginPrinting();

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => invoice.Status.ShouldBe(InvoiceStatus.Processing));
    }

    [Fact]
    public void BeginPrinting_RestampsUpdatedAt()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.UpdatedAt = DateTime.UtcNow.AddDays(-1);

        invoice.BeginPrinting();

        invoice.ShouldSatisfyAllConditions(
            () => invoice.UpdatedAt.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1)),
            () => invoice.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc));
    }

    [Theory]
    [InlineData(InvoiceStatus.Processing)]
    [InlineData(InvoiceStatus.Closed)]
    public void BeginPrinting_FromAnythingButOpen_IsConflict(InvoiceStatus status)
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.Status = status;

        var result = invoice.BeginPrinting();

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Conflict),
            () => result.ErrorMessage.ShouldNotBeNull().ShouldContain(status.ToString()));
    }

    [Fact]
    public void BeginPrinting_Twice_IsConflictAndLeavesProcessing()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();

        var second = invoice.BeginPrinting();

        second.ShouldSatisfyAllConditions(
            () => second.Status.ShouldBe(ResultStatus.Conflict),
            () => invoice.Status.ShouldBe(InvoiceStatus.Processing));
    }
    [Fact]
    public void Close_FromProcessing_ClosesAndStampsClosedAt()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();

        var result = invoice.Close();

        invoice.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => invoice.Status.ShouldBe(InvoiceStatus.Closed),
            () => invoice.ClosedAt.ShouldNotBeNull().Kind.ShouldBe(DateTimeKind.Utc),
            () => invoice.UpdatedAt.ShouldBe(invoice.ClosedAt!.Value));
    }

    [Fact]
    public void Close_WhenAlreadyClosed_IsSuccessAndKeepsTheOriginalClosedAt()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();
        invoice.Close();
        var closedAt = invoice.ClosedAt;

        var second = invoice.Close();

        invoice.ShouldSatisfyAllConditions(
            () => second.IsSuccess.ShouldBeTrue(),
            () => invoice.Status.ShouldBe(InvoiceStatus.Closed),
            () => invoice.ClosedAt.ShouldBe(closedAt));
    }

    [Fact]
    public void Close_FromOpen_IsConflict()
    {
        var invoice = Invoice.Open(3, [Item()]).Value;

        var result = invoice.Close();

        invoice.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Conflict),
            () => result.ErrorMessage.ShouldNotBeNull().ShouldContain("Open"),
            () => invoice.Status.ShouldBe(InvoiceStatus.Open),
            () => invoice.ClosedAt.ShouldBeNull());
    }

    [Fact]
    public void Close_ClearsAFailureReasonFromAnEarlierAttempt()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();
        invoice.FailPrinting("Insufficient balance.");
        invoice.BeginPrinting();

        invoice.Close();

        invoice.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void FailPrinting_FromProcessing_ReopensAndRecordsTheReason()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();

        var result = invoice.FailPrinting("  Insufficient balance for SKU-1.  ");

        invoice.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => invoice.Status.ShouldBe(InvoiceStatus.Open),
            () => invoice.FailureReason.ShouldBe("Insufficient balance for SKU-1."),
            () => invoice.ClosedAt.ShouldBeNull());
    }

    [Fact]
    public void FailPrinting_WhenAlreadyReopened_IsSuccessAndKeepsTheFirstReason()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();
        invoice.FailPrinting("Insufficient balance.");

        var second = invoice.FailPrinting("Something else entirely.");

        invoice.ShouldSatisfyAllConditions(
            () => second.IsSuccess.ShouldBeTrue(),
            () => invoice.Status.ShouldBe(InvoiceStatus.Open),
            () => invoice.FailureReason.ShouldBe("Insufficient balance."));
    }

    [Fact]
    public void FailPrinting_WhenClosed_IsConflictAndLeavesItClosed()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();
        invoice.Close();

        var result = invoice.FailPrinting("A late rejection.");

        invoice.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Conflict),
            () => invoice.Status.ShouldBe(InvoiceStatus.Closed),
            () => invoice.FailureReason.ShouldBeNull());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FailPrinting_WithoutAReason_IsInvalid(string reason)
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();

        var result = invoice.FailPrinting(reason);

        invoice.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => invoice.Status.ShouldBe(InvoiceStatus.Processing));
    }

    [Fact]
    public void FailPrinting_WithAnOverlongReason_IsInvalid()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();

        var result = invoice.FailPrinting(new string('x', Invoice.FailureReasonMaxLength + 1));

        invoice.ShouldSatisfyAllConditions(
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => invoice.Status.ShouldBe(InvoiceStatus.Processing));
    }

    [Fact]
    public void BeginPrinting_ClearsTheFailureReasonFromTheLastAttempt()
    {
        var invoice = Invoice.Open(1, [Item()]).Value;
        invoice.BeginPrinting();
        invoice.FailPrinting("Insufficient balance.");

        invoice.BeginPrinting();

        invoice.ShouldSatisfyAllConditions(
            () => invoice.Status.ShouldBe(InvoiceStatus.Processing),
            () => invoice.FailureReason.ShouldBeNull());
    }
}
