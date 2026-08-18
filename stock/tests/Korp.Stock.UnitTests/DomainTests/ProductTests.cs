using Domain.Common;
using Domain.Product;
using Shouldly;

namespace UnitTests.DomainTests;

public class ProductTests
{
    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = Product.Create("Parafuso sextavado 12mm", "SKU-000000001");

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Status.ShouldBe(ResultStatus.Ok));
    }

    [Fact]
    public void Create_WithValidInput_PopulatesProduct()
    {
        var product = Product.Create("Parafuso sextavado 12mm", "SKU-000000001").Value;

        product.ShouldSatisfyAllConditions(
            () => product.Id.ShouldNotBe(Guid.Empty),
            () => product.Description.ShouldBe("Parafuso sextavado 12mm"),
            () => product.ProductCode.ShouldBe("SKU-000000001"),
            () => product.Active.ShouldBeTrue());
    }

    [Fact]
    public void Create_StampsUtcTimestamps()
    {
        var product = Product.Create("desc", "CODE-1").Value;

        product.ShouldSatisfyAllConditions(
            () => product.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc),
            () => product.UpdatedAt.Kind.ShouldBe(DateTimeKind.Utc),
            () => product.UpdatedAt.ShouldBe(product.CreatedAt));
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var product = Product.Create("  desc  ", "  CODE-1  ").Value;

        product.ShouldSatisfyAllConditions(
            () => product.Description.ShouldBe("desc"),
            () => product.ProductCode.ShouldBe("CODE-1"));
    }

    [Fact]
    public void Create_GeneratesDistinctIds()
    {
        var first = Product.Create("desc", "CODE-1").Value;
        var second = Product.Create("desc", "CODE-2").Value;

        first.Id.ShouldNotBe(second.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDescription_IsInvalid(string? description)
    {
        var result = Product.Create(description!, "SKU-000000001");

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.Status.ShouldBe(ResultStatus.Invalid),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "description"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingProductCode_IsInvalid(string? productCode)
    {
        var result = Product.Create("desc", productCode!);

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "productCode"));
    }

    [Fact]
    public void Create_WithBothFieldsMissing_ReportsBothErrors()
    {
        var result = Product.Create("", "");

        result.ShouldSatisfyAllConditions(
            () => result.ValidationErrors.Count.ShouldBe(2),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "description"),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "productCode"));
    }

    [Fact]
    public void Create_WithDescriptionAtMaxLength_Succeeds()
    {
        var result = Product.Create(new string('a', Product.DescriptionMaxLength), "CODE-1");

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithDescriptionOverMaxLength_IsInvalid()
    {
        var result = Product.Create(new string('a', Product.DescriptionMaxLength + 1), "CODE-1");

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "description"));
    }

    [Fact]
    public void Create_WithProductCodeAtMaxLength_Succeeds()
    {
        var result = Product.Create("desc", new string('a', Product.ProductCodeMaxLength));

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithProductCodeOverMaxLength_IsInvalid()
    {
        var result = Product.Create("desc", new string('a', Product.ProductCodeMaxLength + 1));

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeFalse(),
            () => result.ValidationErrors.ShouldContain(e => e.Field == "productCode"));
    }
}
