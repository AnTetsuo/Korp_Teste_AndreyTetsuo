using Application.Common;
using Domain.Common;

namespace Application.Invoices.ListInvoices;

public sealed class ListInvoicesHandler(IInvoiceReadRepository invoices)
    : IQueryHandler<ListInvoicesQuery, Result<ListInvoicesResponse>>
{
    public async Task<Result<ListInvoicesResponse>> HandleAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var response = await invoices.ListAsync(query, cancellationToken);

        return Result<ListInvoicesResponse>.Success(response);
    }
}
