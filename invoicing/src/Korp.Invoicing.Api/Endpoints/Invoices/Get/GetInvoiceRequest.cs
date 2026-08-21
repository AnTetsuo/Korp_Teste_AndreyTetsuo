using Application.Invoices.GetInvoice;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Invoices.Get;

public sealed record GetInvoiceRequest([FromRoute(Name = "id")] string? InvoiceId)
{
    public GetInvoiceQuery ToQuery() => new(InvoiceIdRules.Parse(InvoiceId));
}
