using Wolverine.Attributes;

namespace Application.Messaging.Contracts;

[MessageIdentity(MessageName)]
public sealed record StockOperationRejected(Guid InvoiceId, string Reason)
{
    public const string MessageName = "stock-operation-rejected";
}
