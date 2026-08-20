using Api.Endpoints.Invoices;
using FluentValidation;
using FluentValidation.TestHelper;
using Shouldly;

namespace UnitTests.ApiTests;

public class InvoiceIdRulesTests
{
    private sealed record AnyRequestWithAnInvoiceId(string? InvoiceId);

    private sealed class AnyValidator : AbstractValidator<AnyRequestWithAnInvoiceId>
    {
        public AnyValidator() => RuleFor(x => x.InvoiceId).MustBeAnInvoiceId();
    }

    private readonly AnyValidator _validator = new();

    [Fact]
    public void AWellFormedId_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new AnyRequestWithAnInvoiceId(Guid.CreateVersion7().ToString()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AMissingId_ReportsRequired(string? segment)
    {
        var result = _validator.TestValidate(new AnyRequestWithAnInvoiceId(segment));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.RequiredMessage);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("123")]
    [InlineData("01994a00-0000-7000-8000")]
    public void AMalformedId_ReportsMalformed(string segment)
    {
        var result = _validator.TestValidate(new AnyRequestWithAnInvoiceId(segment));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.MalformedMessage);
    }

    [Fact]
    public void TheEmptyGuid_ReportsRequired()
    {
        var result = _validator.TestValidate(
            new AnyRequestWithAnInvoiceId(Guid.Empty.ToString()));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.RequiredMessage);
    }

    [Fact]
    public void ABlankId_ReportsOneReason()
    {
        var result = _validator.TestValidate(new AnyRequestWithAnInvoiceId(""));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.RequiredMessage)
            .Only();
    }

    [Fact]
    public void Parse_RoundTripsAWellFormedId()
    {
        var invoiceId = Guid.CreateVersion7();

        InvoiceIdRules.Parse(invoiceId.ToString()).ShouldBe(invoiceId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-guid")]
    public void Parse_OnAnythingElse_YieldsEmpty(string? segment)
    {
        InvoiceIdRules.Parse(segment).ShouldBe(Guid.Empty);
    }
}
