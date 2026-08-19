namespace Domain.Invoices.Items;

public sealed record InvoiceItemDto(
    Guid ProductId,
    string ProductCode,
    string Description,
    int Quantity);
