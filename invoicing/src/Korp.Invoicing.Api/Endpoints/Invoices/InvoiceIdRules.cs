using FluentValidation;

namespace Api.Endpoints.Invoices;

public static class InvoiceIdRules
{
    public const string RequiredMessage = "Invoice id is required.";
    public const string MalformedMessage = "Invoice id must be a valid GUID.";

    public static IRuleBuilderOptions<T, string?> MustBeAnInvoiceId<T>(
        this IRuleBuilderInitial<T, string?> rule) =>
        rule.Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(RequiredMessage)
            .Must(BeAGuid)
            .WithMessage(MalformedMessage)
            .Must(NotBeTheEmptyGuid)
            .WithMessage(RequiredMessage);

    public static Guid Parse(string? invoiceId) =>
        Guid.TryParse(invoiceId, out var parsed) ? parsed : Guid.Empty;

    private static bool BeAGuid(string? value) => Guid.TryParse(value, out _);

    private static bool NotBeTheEmptyGuid(string? value) =>
        Guid.TryParse(value, out var parsed) && parsed != Guid.Empty;
}
