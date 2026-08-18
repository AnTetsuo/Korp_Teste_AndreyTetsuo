using Application.Products.ListProducts.Enums;
using FluentValidation;

namespace Api.Endpoints.Products.List;

public sealed class ListProductsRequestValidator : AbstractValidator<ListProductsRequest>
{
    public const int MaxRows = 100;

    public ListProductsRequestValidator()
    {
        When(x => x.SearchTerm is { Length: not 0 }, () =>
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(255);
        });

        RuleFor(x => x.Rows)
            .InclusiveBetween(5, MaxRows)
            .WithMessage($"Rows must be between 1 and {MaxRows}.");

        When(x => x.OrderBy is not null, () =>
        {
            RuleFor(x => x.OrderBy)
                .IsInEnum()
                .WithMessage($"Order by must be one of: {string.Join(", ", Enum.GetNames<OrderByOptions>())}.");
        });

        When(x => x.Page is not null, () =>
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be at least 1.");
        });
    }
}
