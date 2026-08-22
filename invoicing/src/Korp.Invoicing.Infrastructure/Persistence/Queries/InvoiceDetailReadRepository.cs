using Application.Invoices.GetInvoice;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

internal sealed class InvoiceDetailReadRepository(InvoicingDbContext context)
    : IInvoiceDetailReadRepository
{
    public async Task<GetInvoiceResponse?> GetAsync(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var invoice = await context.Invoices
            .Where(invoice => invoice.Id == invoiceId)
            .Select(invoice => new
            {
                invoice.Id,
                invoice.Number,
                invoice.Status,
                invoice.CreatedAt,
                invoice.UpdatedAt,
                invoice.ClosedAt,
                invoice.FailureReason,
                invoice.FailureCode,
                invoice.FailureLines,
                Items = invoice.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new InvoiceItemLine(
                        item.ProductId, item.ProductCode, item.Description, item.Quantity))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            return null;

        return new GetInvoiceResponse(
            invoice.Id,
            invoice.Number,
            invoice.Status.ToString(),
            invoice.CreatedAt,
            invoice.UpdatedAt,
            invoice.ClosedAt,
            invoice.FailureReason,
            invoice.FailureCode,
            [.. invoice.FailureLines.Select(line =>
                new InvoiceFailureLine(line.ProductId, line.Requested, line.Available))],
            invoice.Items);
    }
}
