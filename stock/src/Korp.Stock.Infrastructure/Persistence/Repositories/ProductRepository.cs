using Domain.Product;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(StockDbContext context) : IProductRepository
{
    public Task<bool> ExistsByCodeAsync(string productCode, CancellationToken cancellationToken = default) =>
        context.Products.AnyAsync(p => p.ProductCode == productCode, cancellationToken);

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(Product product) => context.Products.Add(product);
}
