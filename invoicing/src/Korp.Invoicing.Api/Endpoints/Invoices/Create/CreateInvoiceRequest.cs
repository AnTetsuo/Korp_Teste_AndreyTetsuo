using Application.Invoices.CreateInvoice;

namespace Api.Endpoints.Invoices.Create;

public sealed record CreateInvoiceRequest(
    IReadOnlyList<CreateInvoiceItemRequest>? Items)
{
    public CreateInvoiceCommand ToCommand() =>
        new([.. (Items ?? []).Select(item => item.ToCommandItem())]);
}

public sealed record CreateInvoiceItemRequest(
    Guid? ProductId,
    string ProductCode,
    string Description,
    decimal? Quantity)
{
    internal CreateInvoiceItem ToCommandItem() =>
        new(ProductId ?? Guid.Empty, ProductCode, Description, (int)(Quantity ?? 0m));
}
