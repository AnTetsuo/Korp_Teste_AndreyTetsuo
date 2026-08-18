using FluentValidation;

namespace Api.Endpoints;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().WithMessage("Product code is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        When(x => x.InitialQuantity is not null, () =>
        {
            RuleFor(x => x.InitialQuantity)
                .Must(BeWholeNumber)
                .WithMessage("Initial quantity must be a whole number.");

            RuleFor(x => x.InitialQuantity)
                .Must(BeWithinInt32Range)
                .WithMessage($"Initial quantity must be at most {int.MaxValue}.");
        });
    }

    private static bool BeWholeNumber(decimal? value) =>
        value is not null && decimal.Truncate(value.Value) == value.Value;

    private static bool BeWithinInt32Range(decimal? value) =>
        value is >= int.MinValue and <= int.MaxValue;
}
