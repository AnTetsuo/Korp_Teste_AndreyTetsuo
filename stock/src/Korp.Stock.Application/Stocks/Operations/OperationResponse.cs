namespace Application.Stocks.Operations;

public sealed record OperationResponse(
    Guid InvoiceId,
    IReadOnlyList<OperationLine> Lines);

public sealed record OperationLine(
    Guid ProductId,
    int Quantity,
    int RemainingQuantity);
