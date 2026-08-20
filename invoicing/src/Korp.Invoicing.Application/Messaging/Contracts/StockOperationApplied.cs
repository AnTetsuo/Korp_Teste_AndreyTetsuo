using Wolverine.Attributes;

namespace Application.Messaging.Contracts;

[MessageIdentity(MessageName)]
public sealed record StockOperationApplied(Guid InvoiceId)
{
    public const string MessageName = "stock-operation-applied";
}
