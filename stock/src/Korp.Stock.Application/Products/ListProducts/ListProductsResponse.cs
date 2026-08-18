namespace Application.Products.ListProducts;

public sealed record ListProductsResponse(
    IReadOnlyList<UnitOfProduct> Products,
    int Page,
    int Rows,
    int TotalCount)
{
    public int TotalPages => Rows <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)Rows);
}

public sealed record UnitOfProduct(
    string Description,
    string ProductCode,
    DateTime DateCreated,
    DateTime DateModified,
    int Stock);
