using Domain.Common;
using Domain.Invoices.Enums;
using Domain.Invoices.Items;

namespace Domain.Invoices;

public class Invoice
{
    private readonly List<InvoiceItem> _items = [];

    public Guid Id { get; set; }
    public long Number { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    public static Result<Invoice> Open(long number, IReadOnlyList<InvoiceItemDto> items)
    {
        var errors = new ValidationErrors()
            .Require(number > 0, nameof(number), "Invoice number must be positive.");

        if (items.Count == 0)
            return Result<Invoice>.Invalid(
                errors.Add(nameof(items), "An invoice must have at least one item.").ToArray());

        for (var index = 0; index < items.Count; index++)
            Validate(items[index], index, errors);

        foreach (var duplicate in items
                     .Where(item => item.ProductId != Guid.Empty)
                     .GroupBy(item => item.ProductId)
                     .Where(group => group.Count() > 1))
            errors.Add(nameof(items), $"Product '{duplicate.Key}' appears more than once.");

        if (errors.Any)
            return Result<Invoice>.Invalid(errors.ToArray());

        var now = DateTime.UtcNow;

        var invoice = new Invoice
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Status = InvoiceStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var item in items)
            invoice._items.Add(InvoiceItem.Create(invoice.Id, item));

        return invoice;
    }

    public Result BeginPrinting()
    {
        if (Status != InvoiceStatus.Open)
            return Result.Conflict(
                $"Only an open invoice can be printed; invoice {Number} is {Status}.");

        Status = InvoiceStatus.Processing;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    private static void Validate(InvoiceItemDto item, int index, ValidationErrors errors) =>
        errors
            .RequireId(item.ProductId, $"items[{index}].productId", "Product id")
            .RequireText(item.ProductCode, $"items[{index}].productCode",
                "Product code", InvoiceItem.ProductCodeMaxLength)
            .RequireText(item.Description, $"items[{index}].description",
                "Description", InvoiceItem.DescriptionMaxLength)
            .RequirePositive(item.Quantity, $"items[{index}].quantity", "Quantity");
}
