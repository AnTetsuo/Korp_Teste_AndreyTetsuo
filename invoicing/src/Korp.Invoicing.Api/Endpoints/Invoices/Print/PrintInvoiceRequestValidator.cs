using FluentValidation;

namespace Api.Endpoints.Invoices.Print;

public sealed class PrintInvoiceRequestValidator : AbstractValidator<PrintInvoiceRequest>
{
    public PrintInvoiceRequestValidator()
    {
        RuleFor(x => x.InvoiceId).MustBeAnInvoiceId();
    }
}
