using FluentValidation;

namespace Api.Endpoints.Invoices.Get;

public sealed class GetInvoiceRequestValidator : AbstractValidator<GetInvoiceRequest>
{
    public GetInvoiceRequestValidator()
    {
        RuleFor(x => x.InvoiceId).MustBeAnInvoiceId();
    }
}
