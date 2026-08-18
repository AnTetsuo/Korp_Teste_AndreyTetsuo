namespace Domain.Stocks;

public interface IStockRepository
{
    Task<Stock?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    void Add(Stock stock);
}
