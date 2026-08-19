namespace Application.Invoices.ListInvoices;

public sealed record ListInvoicesResponse(
    IReadOnlyList<UnitOfInvoice> Invoices,
    int Page,
    int Rows,
    int TotalCount)
{
    public int TotalPages => Rows <= 0
        ? 0
        : (int)Math.Ceiling(TotalCount / (double)Rows);
}

public sealed record UnitOfInvoice(
    Guid Id,
    long Number,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int ItemCount,
    int TotalQuantity);
