using Domain.Common;
using Domain.Invoices.Enums;
using Domain.Invoices.Items;

namespace Domain.Invoices;

public class Invoice
{
    public const int FailureReasonMaxLength = 1000;

    private readonly List<InvoiceItem> _items = [];

    public Guid Id { get; set; }
    public long Number { get; set; }
    public InvoiceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? FailureReason { get; set; }

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
        FailureReason = null;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }
    
    public Result Close()
    {
        if (Status == InvoiceStatus.Closed)
            return Result.Success();

        if (Status == InvoiceStatus.Open && FailureReason is null)
            return Result.Conflict(
                $"Only a printing invoice can be closed; invoice {Number} is {Status}.");

        Status = InvoiceStatus.Closed;
        FailureReason = null;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = ClosedAt.Value;

        return Result.Success();
    }

    public Result FailPrinting(string reason)
    {
        var errors = new ValidationErrors()
            .RequireText(reason, nameof(reason), "Failure reason", FailureReasonMaxLength);

        if (errors.Any)
            return Result.Invalid(errors.ToArray());

        if (Status == InvoiceStatus.Closed)
            return Result.Conflict(
                $"Invoice {Number} is already closed and cannot be reopened.");

        if (Status != InvoiceStatus.Processing)
            return Result.Success();

        Status = InvoiceStatus.Open;
        FailureReason = reason.Trim();
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
