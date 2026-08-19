namespace Application.Invoices.ListInvoices;

public interface IInvoiceReadRepository
{
    Task<ListInvoicesResponse> ListAsync(
        ListInvoicesQuery query,
        CancellationToken cancellationToken = default);
}
