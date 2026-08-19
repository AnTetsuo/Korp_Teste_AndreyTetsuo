using Application.Invoices.ListInvoices.Enums;
using Domain.Invoices.Enums;

namespace Application.Invoices.ListInvoices;

public sealed record ListInvoicesQuery(
    long? Number,
    int Rows,
    OrderByOptions? OrderBy,
    bool? Asc,
    InvoiceStatus? Status,
    int? Page);
