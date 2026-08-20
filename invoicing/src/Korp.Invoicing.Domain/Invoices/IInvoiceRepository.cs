namespace Domain.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(Invoice invoice);

    Task<long> NextNumberAsync(CancellationToken cancellationToken = default);
}
