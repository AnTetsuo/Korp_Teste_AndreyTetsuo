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
        if (productId == Guid.Empty)
            return Result<Stock>.Invalid(nameof(productId), "Product id is required.");

        if (quantity < 0)
            return Result<Stock>.Invalid(nameof(quantity), "Initial quantity cannot be negative.");

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
}
