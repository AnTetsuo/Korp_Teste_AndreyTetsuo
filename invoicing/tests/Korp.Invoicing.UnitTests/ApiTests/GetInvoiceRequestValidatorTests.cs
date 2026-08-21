using Api.Endpoints.Invoices;
using Api.Endpoints.Invoices.Get;
using FluentValidation.TestHelper;

namespace UnitTests.ApiTests;

public class GetInvoiceRequestValidatorTests
{
    private readonly GetInvoiceRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new GetInvoiceRequest(Guid.CreateVersion7().ToString()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvoiceId_UsesTheSharedRule()
    {
        var result = _validator.TestValidate(new GetInvoiceRequest("not-a-guid"));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.MalformedMessage);
    }

    [Fact]
    public void InvoiceId_WhenTheEmptyGuid_IsRejected()
    {
        var result = _validator.TestValidate(new GetInvoiceRequest(Guid.Empty.ToString()));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.RequiredMessage);
    }
}
