namespace Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string ProductCode,
    string Description,
    int InitialQuantity);
