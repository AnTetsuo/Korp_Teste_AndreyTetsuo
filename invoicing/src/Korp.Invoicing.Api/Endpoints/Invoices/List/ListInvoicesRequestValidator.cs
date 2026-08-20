using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices.Enums;
using FluentValidation;

namespace Api.Endpoints.Invoices.List;

public sealed class ListInvoicesRequestValidator : AbstractValidator<ListInvoicesRequest>
{
    public const int MinRows = 5;
    public const int MaxRows = 100;

    public ListInvoicesRequestValidator()
    {
        RuleFor(x => x.Rows)
            .InclusiveBetween(MinRows, MaxRows)
            .WithMessage($"Rows must be between {MinRows} and {MaxRows}.");

        When(x => x.Number is not null, () =>
        {
            RuleFor(x => x.Number)
                .GreaterThan(0)
                .WithMessage("Number must be greater than zero.");
        });

        When(x => x.OrderBy is not null, () =>
        {
            RuleFor(x => x.OrderBy)
                .IsInEnum()
                .WithMessage($"Order by must be one of: {string.Join(", ", Enum.GetNames<OrderByOptions>())}.");
        });

        When(x => x.Status is not null, () =>
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage($"Status must be one of: {string.Join(", ", Enum.GetNames<InvoiceStatus>())}.");
        });

        When(x => x.Page is not null, () =>
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");
        });
    }
}
