namespace Application.Invoices.CreateInvoice;

public sealed record CreateInvoiceResponse(
    Guid Id,
    long Number,
    string Status,
    DateTime CreatedAt,
    IReadOnlyList<InvoiceLine> Items);

public sealed record InvoiceLine(
    Guid ProductId,
    string ProductCode,
    string Description,
    int Quantity);
