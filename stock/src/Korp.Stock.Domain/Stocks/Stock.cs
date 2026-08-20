using Domain.Common;
using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;

namespace Domain.Stocks;

public class Stock
{
    private readonly List<Transaction> _transactions = [];

    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

    public static Result<Stock> Init(Guid productId, int quantity, Guid? referenceId = null)
    {
        var errors = new ValidationErrors()
            .RequireId(productId, nameof(productId), "Product id")
            .Require(quantity >= 0, nameof(quantity), "Initial quantity cannot be negative.");

        if (errors.Any)
            return Result<Stock>.Invalid(errors.ToArray());

        var now = DateTime.UtcNow;

        var stock = new Stock
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            Quantity = quantity,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (quantity > 0)
            stock._transactions.Add(Transaction.RecordMovement(
                stock.Id, quantity, TransactionType.Initial, referenceId));

        return stock;
    }

    public Result<Transaction> Operate(int quantity, Guid entityReferenceId)
    {
        var errors = new ValidationErrors()
            .RequirePositive(quantity, nameof(quantity), "Operation quantity")
            .Require(entityReferenceId != Guid.Empty, nameof(entityReferenceId),
                "An operation must name the reference that issued it.");

        if (errors.Any)
            return Result<Transaction>.Invalid(errors.ToArray());

        if (Quantity < quantity)
            return Result<Transaction>.Conflict(
                $"Insufficient balance: {Quantity} available, {quantity} requested.");

        Quantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        var movement = Transaction.RecordMovement(
            Id, quantity, TransactionType.InvoiceOutput, entityReferenceId);

        _transactions.Add(movement);

        return movement;
    }
}
