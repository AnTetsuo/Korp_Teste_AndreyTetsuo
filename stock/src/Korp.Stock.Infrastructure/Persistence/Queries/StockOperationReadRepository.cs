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
        var rows = await (
                from movement in context.Transactions
                join reference in context.EntityReferences
                    on movement.ReferenceId equals reference.Id
                join stock in context.Stocks
                    on movement.StockId equals stock.Id
                where reference.EntityType == EntityType.Invoice
                      && reference.ReferenceId == invoiceId
                      && movement.TransactionType == TransactionType.InvoiceOutput
                orderby movement.CreatedAt, stock.ProductId
                select new
                {
                    stock.ProductId,
                    movement.Quantity,
                    RemainingQuantity = stock.Quantity
                })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return null;

        var lines = rows
            .Select(row => new OperationLine(row.ProductId, row.Quantity, row.RemainingQuantity))
            .ToList();

        return new OperationResponse(invoiceId, lines);
    }
}
