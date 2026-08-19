namespace Domain.Invoices;

public interface IInvoiceRepository
{
    void Add(Invoice invoice);

    Task<long> NextNumberAsync(CancellationToken cancellationToken = default);
}
