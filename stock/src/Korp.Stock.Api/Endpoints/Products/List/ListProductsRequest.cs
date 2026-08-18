using Application.Products.ListProducts;
using Application.Products.ListProducts.Enums;

namespace Api.Endpoints.Products.List;

public sealed record ListProductsRequest(
    string? SearchTerm,
    int Rows,
    OrderByOptions? OrderBy,
    bool? Asc,
    bool? Active,
    int? Page)
{
    public ListProductsQuery ToQuery() =>
        new(SearchTerm, Rows, OrderBy, Asc, Active, Page);
}
