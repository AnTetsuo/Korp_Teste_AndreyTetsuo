namespace Application.Invoices.CreateInvoice;

public sealed record CreateInvoiceCommand(
    IReadOnlyList<CreateInvoiceItem> Items);

public sealed record CreateInvoiceItem(
    Guid ProductId,
    string ProductCode,
    string Description,
    int Quantity);
