namespace Application.Invoices.GetInvoice;

public interface IInvoiceDetailReadRepository
{
    Task<GetInvoiceResponse?> GetAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
