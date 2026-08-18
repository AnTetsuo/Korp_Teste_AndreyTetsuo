using Api.Endpoints;
using Api.Endpoints.Products.Create;
using FluentValidation.TestHelper;

namespace UnitTests.ApiTests;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequest Request(
        string productCode = "SKU-000000001",
        string description = "Parafuso sextavado 12mm",
        decimal? initialQuantity = 1) =>
        new(productCode, description, initialQuantity);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(Request());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductCode_WhenMissing_HasError(string? productCode)
    {
        var result = _validator.TestValidate(Request(productCode: productCode!));

        result.ShouldHaveValidationErrorFor(x => x.ProductCode)
            .WithErrorMessage("Product code is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_WhenMissing_HasError(string? description)
    {
        var result = _validator.TestValidate(Request(description: description!));

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description is required.");
    }

    [Fact]
    public void InitialQuantity_WhenOmitted_HasNoError()
    {
        var result = _validator.TestValidate(Request(initialQuantity: null));

        result.ShouldNotHaveValidationErrorFor(x => x.InitialQuantity);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.5)]
    [InlineData(-2.75)]
    public void InitialQuantity_WhenFractional_HasError(decimal initialQuantity)
    {
        var result = _validator.TestValidate(Request(initialQuantity: initialQuantity));

        result.ShouldHaveValidationErrorFor(x => x.InitialQuantity)
            .WithErrorMessage("Initial quantity must be a whole number.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2147483647)]
    public void InitialQuantity_WhenWholeAndInRange_HasNoError(decimal initialQuantity)
    {
        var result = _validator.TestValidate(Request(initialQuantity: initialQuantity));

        result.ShouldNotHaveValidationErrorFor(x => x.InitialQuantity);
    }

    [Theory]
    [InlineData(2147483648)]
    [InlineData(99999999999)]
    [InlineData(-2147483649)]
    public void InitialQuantity_WhenOutOfInt32Range_HasError(decimal initialQuantity)
    {
        var result = _validator.TestValidate(Request(initialQuantity: initialQuantity));

        result.ShouldHaveValidationErrorFor(x => x.InitialQuantity)
            .WithErrorMessage($"Initial quantity must be at most {int.MaxValue}.");
    }

    [Fact]
    public void EmptyRequest_ReportsCodeAndDescriptionOnly()
    {
        var result = _validator.TestValidate(new CreateProductRequest(null!, null!, null));

        result.ShouldHaveValidationErrorFor(x => x.ProductCode);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldNotHaveValidationErrorFor(x => x.InitialQuantity);
    }
}
