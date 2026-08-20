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
        var errors = new List<ValidationError>();

        if (number <= 0)
            errors.Add(new ValidationError(nameof(number), "Invoice number must be positive."));

        if (items.Count == 0)
        {
            errors.Add(new ValidationError(
                nameof(items), "An invoice must have at least one item."));

            return Result<Invoice>.Invalid([.. errors]);
        }

        for (var index = 0; index < items.Count; index++)
            ValidateItem(items[index], index, errors);

        var duplicates = items
            .Where(item => item.ProductId != Guid.Empty)
            .GroupBy(item => item.ProductId)
            .Where(group => group.Count() > 1);

        foreach (var duplicate in duplicates)
            errors.Add(new ValidationError(
                nameof(items), $"Product '{duplicate.Key}' appears more than once."));

        if (errors.Count > 0)
            return Result<Invoice>.Invalid([.. errors]);

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

    private static void ValidateItem(
        InvoiceItemDto item,
        int index,
        List<ValidationError> errors)
    {
        if (item.ProductId == Guid.Empty)
            errors.Add(new ValidationError(
                $"items[{index}].productId", "Product id is required."));

        if (string.IsNullOrWhiteSpace(item.ProductCode))
            errors.Add(new ValidationError(
                $"items[{index}].productCode", "Product code is required."));
        else if (item.ProductCode.Trim().Length > InvoiceItem.ProductCodeMaxLength)
            errors.Add(new ValidationError(
                $"items[{index}].productCode",
                $"Product code must be at most {InvoiceItem.ProductCodeMaxLength} characters."));

        if (string.IsNullOrWhiteSpace(item.Description))
            errors.Add(new ValidationError(
                $"items[{index}].description", "Description is required."));
        else if (item.Description.Trim().Length > InvoiceItem.DescriptionMaxLength)
            errors.Add(new ValidationError(
                $"items[{index}].description",
                $"Description must be at most {InvoiceItem.DescriptionMaxLength} characters."));

        if (item.Quantity <= 0)
            errors.Add(new ValidationError(
                $"items[{index}].quantity", "Quantity must be greater than zero."));
    }
}
