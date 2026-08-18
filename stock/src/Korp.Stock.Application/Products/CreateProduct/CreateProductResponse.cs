namespace Application.Products.CreateProduct;

public sealed record CreateProductResponse(
    Guid Id,
    string ProductCode,
    string Description,
    int Quantity);
