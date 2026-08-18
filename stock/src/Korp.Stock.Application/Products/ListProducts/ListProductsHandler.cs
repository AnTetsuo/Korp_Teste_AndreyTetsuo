using Application.Common;
using Domain.Common;

namespace Application.Products.ListProducts;

public sealed class ListProductsHandler(IProductReadRepository products)
    : IQueryHandler<ListProductsQuery, Result<ListProductsResponse>>
{
    public async Task<Result<ListProductsResponse>> HandleAsync(
        ListProductsQuery query,
        CancellationToken cancellationToken)
    {
        var response = await products.ListAsync(query, cancellationToken);

        return Result<ListProductsResponse>.Success(response);
    }
}
