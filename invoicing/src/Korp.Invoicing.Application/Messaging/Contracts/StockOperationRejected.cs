using Wolverine.Attributes;

namespace Application.Messaging.Contracts;

[MessageIdentity(MessageName)]
public sealed record StockOperationRejected(Guid InvoiceId, string Reason)
{
    public const string MessageName = "stock-operation-rejected";

    public const string InsufficientStock = "insufficient_stock";
    public const string InvalidRequest = "invalid_request";

    public string? Code { get; init; }

    public IReadOnlyList<RejectedLine> Lines { get; init; } = [];
}

public sealed record RejectedLine(Guid ProductId, int Requested, int Available);
