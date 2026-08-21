using Api.Endpoints.Invoices.Create;
using Api.Endpoints.Invoices.Get;
using Api.Endpoints.Invoices.List;
using Api.Endpoints.Invoices.Print;
using Api.Extensions;
using Application.Invoices.CreateInvoice;
using Application.Invoices.GetInvoice;
using Application.Invoices.ListInvoices;
using Application.Invoices.PrintInvoice;

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

        group.MapGet("/{id}", async (
                [AsParameters] GetInvoiceRequest request,
                GetInvoiceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToQuery(), cancellationToken);

                return result.ToHttpResult(Results.Ok);
            })
            .AddEndpointFilter<ValidationFilter<GetInvoiceRequest>>()
            .WithName("GetInvoice")
            .WithSummary("Reads one invoice with its line items and print outcome.")
            .WithDescription(
                "This is what a client polls while the status is Processing. FailureReason " +
                "carries the reason stock rejected the last print and is cleared when the " +
                "invoice is printed again; ClosedAt is set once stock confirms.")
            .Produces<GetInvoiceResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/print", async (
                [AsParameters] PrintInvoiceRequest request,
                PrintInvoiceHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);

                return result.ToHttpResult(response => Results.Accepted(
                    $"/invoices/{response.Id}", response));
            })
            .AddEndpointFilter<ValidationFilter<PrintInvoiceRequest>>()
            .WithName("PrintInvoice")
            .WithSummary("Moves an invoice to Processing and asks stock to draw its lines down.")
            .WithDescription(
                "202 Accepted, not 200: the balances have not moved yet. The status change and " +
                "the outgoing message commit as one transaction, so the request is durable the " +
                "moment it returns — but stock applies it asynchronously, and the invoice reaches " +
                "Closed only when stock replies. Poll GET /invoices/{id} while the status is " +
                "Processing. 409 if the invoice is not Open, including a double-clicked Imprimir.")
            .Produces<PrintInvoiceResponse>(StatusCodes.Status202Accepted)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }
}
