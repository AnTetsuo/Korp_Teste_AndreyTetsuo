using Application.Invoices.PrintInvoice;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.Invoices.Print;

public sealed record PrintInvoiceRequest([FromRoute(Name = "id")] string? InvoiceId)
{
    public PrintInvoiceCommand ToCommand() => new(InvoiceIdRules.Parse(InvoiceId));
}
