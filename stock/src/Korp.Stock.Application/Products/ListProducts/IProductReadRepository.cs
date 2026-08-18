namespace Application.Products.ListProducts;

public interface IProductReadRepository
{
    Task<ListProductsResponse> ListAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken = default);
}
