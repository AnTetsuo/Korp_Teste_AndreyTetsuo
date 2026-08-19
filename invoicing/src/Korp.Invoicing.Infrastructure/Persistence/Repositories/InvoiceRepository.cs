using Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

internal sealed class InvoiceRepository(InvoicingDbContext context) : IInvoiceRepository
{
    public void Add(Invoice invoice) => context.Invoices.Add(invoice);

    public async Task<long> NextNumberAsync(CancellationToken cancellationToken = default)
    {
        var row = await context.Database
            .SqlQueryRaw<InvoiceNumberRow>(
                $"SELECT nextval('{InvoicingDbContext.QualifiedInvoiceNumberSequence}') AS next_value")
            .SingleAsync(cancellationToken);

        return row.NextValue;
    }
}

internal sealed class InvoiceNumberRow
{
    public long NextValue { get; set; }
}
