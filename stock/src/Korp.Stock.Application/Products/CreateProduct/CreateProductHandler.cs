using Application.Common;
using Domain.Common;
using Domain.Product;
using Domain.Stocks;

namespace Application.Products.CreateProduct;

public sealed class CreateProductHandler(
    IProductRepository products,
    IStockRepository stocks,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateProductCommand, Result<CreateProductResponse>>
{
    public async Task<Result<CreateProductResponse>> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        if (await products.ExistsByCodeAsync(command.ProductCode, cancellationToken))
            return Result<CreateProductResponse>.Conflict(
                $"A product with code '{command.ProductCode}' already exists.");

        var productResult = Product.Create(command.Description, command.ProductCode);
        if (!productResult.IsSuccess)
            return Result<CreateProductResponse>.Invalid([.. productResult.ValidationErrors]);

        var product = productResult.Value;

        var stockResult = Stock.Init(product.Id, command.InitialQuantity);
        if (!stockResult.IsSuccess)
            return Result<CreateProductResponse>.Invalid([.. stockResult.ValidationErrors]);

        var stock = stockResult.Value;

        products.Add(product);
        stocks.Add(stock);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateProductResponse>.Created(new CreateProductResponse(
            product.Id,
            product.ProductCode,
            product.Description,
            stock.Quantity));
    }
}
