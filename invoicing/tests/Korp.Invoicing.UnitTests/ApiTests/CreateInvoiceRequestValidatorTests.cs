using Api.Endpoints.Invoices.Create;
using FluentValidation.TestHelper;
using Shouldly;

namespace UnitTests.ApiTests;

public class CreateInvoiceRequestValidatorTests
{
    private readonly CreateInvoiceRequestValidator _validator = new();

    private static CreateInvoiceItemRequest Line(
        Guid? productId = null,
        string productCode = "SKU-000000001",
        string description = "Parafuso sextavado 12mm",
        decimal? quantity = 2) =>
        new(productId ?? Guid.CreateVersion7(), productCode, description, quantity);

    private static CreateInvoiceRequest Request(params CreateInvoiceItemRequest[] items) =>
        new(items.Length == 0 ? [Line()] : items);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(Request());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Items_WhenNull_HasError()
    {
        var result = _validator.TestValidate(new CreateInvoiceRequest(null));

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("An invoice must have at least one item.");
    }

    [Fact]
    public void Items_WhenEmpty_HasError()
    {
        var result = _validator.TestValidate(new CreateInvoiceRequest([]));

        result.ShouldHaveValidationErrorFor(x => x.Items)
            .WithErrorMessage("An invoice must have at least one item.");
    }

    [Fact]
    public void ProductId_WhenEmpty_HasError()
    {
        var result = _validator.TestValidate(Request(Line(productId: Guid.Empty)));

        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("Product id is required.");
    }

    [Fact]
    public void ProductId_WhenNull_HasError()
    {
        var result = _validator.TestValidate(Request(
            new CreateInvoiceItemRequest(null, "SKU-000000001", "Parafuso", 2)));

        result.ShouldHaveValidationErrorFor("Items[0].ProductId")
            .WithErrorMessage("Product id is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductCode_WhenMissing_HasError(string? productCode)
    {
        var result = _validator.TestValidate(Request(Line(productCode: productCode!)));

        result.ShouldHaveValidationErrorFor("Items[0].ProductCode")
            .WithErrorMessage("Product code is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_WhenMissing_HasError(string? description)
    {
        var result = _validator.TestValidate(Request(Line(description: description!)));

        result.ShouldHaveValidationErrorFor("Items[0].Description")
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void Quantity_WhenOmitted_HasError()
    {
        var result = _validator.TestValidate(Request(Line(quantity: null)));

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity is required.");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.5)]
    [InlineData(-2.75)]
    public void Quantity_WhenFractional_HasError(decimal quantity)
    {
        var result = _validator.TestValidate(Request(Line(quantity: quantity)));

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity must be a whole number.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Quantity_WhenNotPositive_HasError(decimal quantity)
    {
        var result = _validator.TestValidate(Request(Line(quantity: quantity)));

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage("Quantity must be greater than zero.");
    }

    [Fact]
    public void Quantity_WhenFractionalAndNegative_ReportsOnlyTheFirstFailure()
    {
        var result = _validator.TestValidate(Request(Line(quantity: -2.75m)));

        result.Errors
            .Where(error => error.PropertyName == "Items[0].Quantity")
            .Select(error => error.ErrorMessage)
            .ShouldBe(["Quantity must be a whole number."]);
    }

    [Fact]
    public void Quantity_BeyondInt32Range_HasError()
    {
        var result = _validator.TestValidate(Request(Line(quantity: (decimal)int.MaxValue + 1)));

        result.ShouldHaveValidationErrorFor("Items[0].Quantity")
            .WithErrorMessage($"Quantity must be at most {int.MaxValue}.");
    }

    [Fact]
    public void EveryFailingLine_IsReported()
    {
        var result = _validator.TestValidate(Request(
            Line(quantity: 0),
            Line(productCode: "")));

        result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        result.ShouldHaveValidationErrorFor("Items[1].ProductCode");
    }
}
