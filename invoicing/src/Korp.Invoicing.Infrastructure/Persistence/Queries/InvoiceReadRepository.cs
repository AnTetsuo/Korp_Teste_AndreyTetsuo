using Application.Invoices.ListInvoices;
using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Queries;

internal sealed class InvoiceReadRepository(InvoicingDbContext context) : IInvoiceReadRepository
{
    public async Task<ListInvoicesResponse> ListAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var filtered = context.Invoices.AsNoTracking();

        if (query.Number is { } number)
            filtered = filtered.Where(invoice => invoice.Number == number);

        if (query.Status is { } status)
            filtered = filtered.Where(invoice => invoice.Status == status);

        var totalCount = await filtered.CountAsync(cancellationToken);

        var page = query.Page ?? 1;

        var rows = await Order(filtered, query.OrderBy, query.Asc ?? false)
            .Skip((page - 1) * query.Rows)
            .Take(query.Rows)
            .Select(invoice => new
            {
                invoice.Id,
                invoice.Number,
                invoice.Status,
                invoice.CreatedAt,
                invoice.UpdatedAt,
                ItemCount = invoice.Items.Count,
                TotalQuantity = invoice.Items.Sum(item => (int?)item.Quantity) ?? 0
            })
            .ToListAsync(cancellationToken);

        var invoices = rows
            .Select(row => new UnitOfInvoice(
                row.Id,
                row.Number,
                row.Status.ToString(),
                row.CreatedAt,
                row.UpdatedAt,
                row.ItemCount,
                row.TotalQuantity))
            .ToList();

        return new ListInvoicesResponse(invoices, page, query.Rows, totalCount);
    }

    private static IQueryable<Invoice> Order(
        IQueryable<Invoice> invoices,
        OrderByOptions? orderBy,
        bool ascending) =>
        (orderBy, ascending) switch
        {
            (OrderByOptions.CreatedAt, true) =>
                invoices.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id),
            (OrderByOptions.CreatedAt, false) =>
                invoices.OrderByDescending(i => i.CreatedAt).ThenBy(i => i.Id),
            (OrderByOptions.UpdatedAt, true) =>
                invoices.OrderBy(i => i.UpdatedAt).ThenBy(i => i.Id),
            (OrderByOptions.UpdatedAt, false) =>
                invoices.OrderByDescending(i => i.UpdatedAt).ThenBy(i => i.Id),
            (OrderByOptions.Status, true) =>
                invoices.OrderBy(i => i.Status).ThenBy(i => i.Id),
            (OrderByOptions.Status, false) =>
                invoices.OrderByDescending(i => i.Status).ThenBy(i => i.Id),
            (_, true) => invoices.OrderBy(i => i.Number),
            _ => invoices.OrderByDescending(i => i.Number)
        };
}
