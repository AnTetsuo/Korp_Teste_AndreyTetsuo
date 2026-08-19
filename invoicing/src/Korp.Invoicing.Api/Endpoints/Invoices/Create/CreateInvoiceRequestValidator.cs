using FluentValidation;

namespace Api.Endpoints.Invoices.Create;

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("An invoice must have at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateInvoiceItemRequestValidator());
    }
}

internal sealed class CreateInvoiceItemRequestValidator
    : AbstractValidator<CreateInvoiceItemRequest>
{
    public CreateInvoiceItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotNull().WithMessage("Product id is required.")
            .NotEqual(Guid.Empty).WithMessage("Product id is required.");

        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Quantity)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Quantity is required.")
            .Must(BeWholeNumber).WithMessage("Quantity must be a whole number.")
            .Must(BeWithinInt32Range).WithMessage($"Quantity must be at most {int.MaxValue}.")
            .Must(BePositive).WithMessage("Quantity must be greater than zero.");
    }

    private static bool BeWholeNumber(decimal? value) =>
        decimal.Truncate(value!.Value) == value.Value;

    private static bool BeWithinInt32Range(decimal? value) =>
        value!.Value is >= int.MinValue and <= int.MaxValue;

    private static bool BePositive(decimal? value) => value!.Value > 0m;
}
