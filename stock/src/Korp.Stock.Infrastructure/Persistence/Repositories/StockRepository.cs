using Domain.Stocks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class StockRepository(StockDbContext context) : IStockRepository
{
    public Task<Stock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        context.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);

    public async Task<IReadOnlyList<Stock>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
            return [];

        return await context.Stocks
            .Include(s => s.Transactions)
            .Where(s => productIds.Contains(s.ProductId))
            .OrderBy(s => s.ProductId)
            .ToListAsync(cancellationToken);
    }

    public void Add(Stock stock) => context.Stocks.Add(stock);
}
