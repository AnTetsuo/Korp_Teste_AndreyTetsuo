namespace Application.Invoices.PrintInvoice;

public sealed record PrintInvoiceResponse(
    Guid Id,
    long Number,
    string Status,
    DateTime UpdatedAt);
