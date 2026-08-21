using Application.Common;
using Domain.Common;

namespace Application.Invoices.GetInvoice;

public sealed class GetInvoiceHandler(IInvoiceDetailReadRepository invoices)
    : IQueryHandler<GetInvoiceQuery, Result<GetInvoiceResponse>>
{
    public async Task<Result<GetInvoiceResponse>> HandleAsync(
        GetInvoiceQuery query,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetAsync(query.InvoiceId, cancellationToken);

        return invoice is not null
            ? Result<GetInvoiceResponse>.Success(invoice)
            : Result<GetInvoiceResponse>.NotFound($"Invoice '{query.InvoiceId}' was not found.");
    }
}
