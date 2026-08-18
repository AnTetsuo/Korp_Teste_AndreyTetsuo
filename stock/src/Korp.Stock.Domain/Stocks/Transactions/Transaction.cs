using Domain.Stocks.Transactions.Enums;

namespace Domain.Stocks.Transactions;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid StockId { get; set; }
    public TransactionType TransactionType { get; set; }
    public int Quantity { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }

    internal static Transaction RecordMovement(
        Guid stockId,
        int quantity,
        TransactionType transactionType,
        Guid? referenceId)
    {
        return new Transaction
        {
            Id = Guid.CreateVersion7(),
            StockId = stockId,
            Quantity = quantity,
            TransactionType = transactionType,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
