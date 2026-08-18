using Application.Products.CreateProduct;

namespace Api.Endpoints.Products.Create;

public sealed record CreateProductRequest(
    string ProductCode,
    string Description,
    decimal? InitialQuantity)
{
    public CreateProductCommand ToCommand() =>
        new(ProductCode, Description, (int)(InitialQuantity ?? 0m));
}
