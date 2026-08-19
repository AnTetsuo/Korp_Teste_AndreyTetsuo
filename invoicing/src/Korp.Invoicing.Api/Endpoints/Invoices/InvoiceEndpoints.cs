using Api.Endpoints.Invoices.Create;
using Api.Endpoints.Invoices.List;
using Api.Extensions;
using Application.Invoices.CreateInvoice;
using Application.Invoices.ListInvoices;

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

        group.MapGet("/", async (
                [AsParameters] ListInvoicesRequest request,
                ListInvoicesHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToQuery(), cancellationToken);

                return result.ToHttpResult(Results.Ok);
            })
            .AddEndpointFilter<ValidationFilter<ListInvoicesRequest>>()
            .WithName("ListInvoices")
            .WithSummary("Lists invoices with their line counts and totals.")
            .Produces<ListInvoicesResponse>()
            .ProducesValidationProblem();
    }
}
