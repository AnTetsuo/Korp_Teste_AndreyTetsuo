using Application.Invoices.ListInvoices;
using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices.Enums;

namespace Api.Endpoints.Invoices.List;

public sealed record ListInvoicesRequest(
    long? Number,
    int Rows,
    OrderByOptions? OrderBy,
    bool? Asc,
    InvoiceStatus? Status,
    int? Page)
{
    public ListInvoicesQuery ToQuery() =>
        new(Number, Rows, OrderBy, Asc, Status, Page);
}
