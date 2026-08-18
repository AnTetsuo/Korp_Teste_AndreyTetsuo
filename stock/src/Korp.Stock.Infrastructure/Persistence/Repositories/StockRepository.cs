using Domain.Stocks;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class StockRepository(StockDbContext context) : IStockRepository
{
    public Task<Stock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default) =>
        context.Stocks.FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);

    public void Add(Stock stock) => context.Stocks.Add(stock);
}
