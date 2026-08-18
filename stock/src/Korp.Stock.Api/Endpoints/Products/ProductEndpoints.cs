using Api.Endpoints.Products.Create;
using Api.Endpoints.Products.List;
using Api.Extensions;
using Application.Products.CreateProduct;
using Application.Products.ListProducts;

namespace Api.Endpoints.Products;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/products")
            .WithTags("Products");

        group.MapPost("/", async (
                CreateProductRequest request,
                CreateProductHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

                return result.ToHttpResult(response =>
                    Results.Created($"/products/{response.Id}", response));
            })
            .AddEndpointFilter<ValidationFilter<CreateProductRequest>>()
            .WithName("CreateProduct")
            .WithSummary("Registers a product and opens its stock balance.")
            .Produces<CreateProductResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", async (
                [AsParameters] ListProductsRequest request,
                ListProductsHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToQuery(), cancellationToken);

                return result.ToHttpResult(Results.Ok);
            })
            .AddEndpointFilter<ValidationFilter<ListProductsRequest>>()
            .WithName("ListProducts")
            .WithSummary("Lists products with their current stock balance.")
            .Produces<ListProductsResponse>()
            .ProducesValidationProblem();
    }
}
