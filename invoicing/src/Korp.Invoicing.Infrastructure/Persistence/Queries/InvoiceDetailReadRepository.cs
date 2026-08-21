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
        return await context.Invoices
            .Where(invoice => invoice.Id == invoiceId)
            .Select(invoice => new GetInvoiceResponse
            (
                invoice.Id,
                invoice.Number,
                invoice.Status.ToString(),
                invoice.CreatedAt,
                invoice.UpdatedAt,
                invoice.ClosedAt,
                invoice.FailureReason,
                invoice.Items
                    .OrderBy(item => item.Id)
                    .Select(item => new InvoiceItemLine(
                        item.ProductId, item.ProductCode, item.Description, item.Quantity))
                    .ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
