using Api.Endpoints.Invoices;
using Api.Endpoints.Invoices.Print;
using FluentValidation.TestHelper;

namespace UnitTests.ApiTests;

public class PrintInvoiceRequestValidatorTests
{
    private readonly PrintInvoiceRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(
            new PrintInvoiceRequest(Guid.CreateVersion7().ToString()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvoiceId_UsesTheSharedRule()
    {
        var result = _validator.TestValidate(new PrintInvoiceRequest("not-a-guid"));

        result.ShouldHaveValidationErrorFor(x => x.InvoiceId)
            .WithErrorMessage(InvoiceIdRules.MalformedMessage);
    }
}
