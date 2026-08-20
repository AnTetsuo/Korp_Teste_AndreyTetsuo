using Application.Stocks.Operations;
using Domain.Stocks.Transactions.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

internal sealed class StockOperationReadRepository(StockDbContext context)
    : IStockOperationReadRepository
{
    public async Task<OperationResponse?> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var lines = await context.Transactions
            .Where(movement =>
                movement.TransactionType == TransactionType.InvoiceOutput &&
                movement.EntityReference != null &&
                movement.EntityReference.EntityType == EntityType.Invoice &&
                movement.EntityReference.ReferenceId == invoiceId)
            .OrderBy(movement => movement.CreatedAt)
            .ThenBy(movement => movement.Stock.ProductId)
            .Select(movement => new OperationLine(
                movement.Stock.ProductId,
                movement.Quantity,
                movement.Stock.Quantity))
            .ToListAsync(cancellationToken);

        return lines.Count == 0 ? null : new OperationResponse(invoiceId, lines);
    }
}
