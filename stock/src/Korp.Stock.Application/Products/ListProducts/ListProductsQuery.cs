using Application.Products.ListProducts.Enums;

namespace Application.Products.ListProducts;

public sealed record ListProductsQuery(
    string? SearchTerm,
    int Rows, 
    OrderByOptions? OrderBy,
    bool? Asc,
    bool? Active,
    int? Page);