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
}
