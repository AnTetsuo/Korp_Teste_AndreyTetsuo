namespace Application.Invoices.GetInvoice;

public sealed record GetInvoiceResponse(
    Guid Id,
    long Number,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ClosedAt,
    string? FailureReason,
    IReadOnlyList<InvoiceItemLine> Items);

public sealed record InvoiceItemLine(
    Guid ProductId,
    string ProductCode,
    string Description,
    int Quantity);
