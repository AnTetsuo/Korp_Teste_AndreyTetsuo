namespace Domain.Invoices.Items;

public class InvoiceItem
{
    public const int ProductCodeMaxLength = 16;
    public const int DescriptionMaxLength = 255;

    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }

    internal static InvoiceItem Create(Guid invoiceId, InvoiceItemDto item) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = invoiceId,
            ProductId = item.ProductId,
            ProductCode = item.ProductCode.Trim(),
            Description = item.Description.Trim(),
            Quantity = item.Quantity
        };
}
