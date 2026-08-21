using Wolverine.Attributes;

namespace Application.Messaging.Contracts;

[MessageIdentity(MessageName)]
public sealed record PrintTimeoutCheck(Guid InvoiceId, int Attempt)
{
    public const string MessageName = "print-timeout-check";
}
