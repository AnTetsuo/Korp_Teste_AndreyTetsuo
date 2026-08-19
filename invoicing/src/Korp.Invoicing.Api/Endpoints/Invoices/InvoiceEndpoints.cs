using Api.Endpoints.Invoices.Create;
using Api.Extensions;
using Application.Invoices.CreateInvoice;

namespace Api.Endpoints.Invoices;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/invoices")
            .WithTags("Invoices");

        group.MapPost("/", async (
                CreateInvoiceRequest request,
                CreateInvoiceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

                return result.ToHttpResult(response =>
                    Results.Created($"/invoices/{response.Id}", response));
            })
            .AddEndpointFilter<ValidationFilter<CreateInvoiceRequest>>()
            .WithName("CreateInvoice")
            .WithSummary("Opens an invoice with its product lines.")
            .Produces<CreateInvoiceResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
    }
}
