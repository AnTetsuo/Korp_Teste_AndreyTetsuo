namespace Domain.Product;

public interface IProductRepository
{
    Task<bool> ExistsByCodeAsync(string productCode, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Product product);
}
