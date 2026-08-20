namespace Application.Stocks.Operations;

public interface IStockOperationReadRepository
{
    Task<OperationResponse?> GetByInvoiceIdAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}
