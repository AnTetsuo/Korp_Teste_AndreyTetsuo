using Api.Extensions;
using Application.Products.CreateProduct;

namespace Api.Endpoints;

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
    }
}
