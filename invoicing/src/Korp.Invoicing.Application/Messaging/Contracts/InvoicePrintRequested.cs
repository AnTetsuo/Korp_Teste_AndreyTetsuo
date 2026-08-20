using Wolverine.Attributes;

namespace Application.Messaging.Contracts;

[MessageIdentity(MessageName)]
public sealed record InvoicePrintRequested(
    Guid InvoiceId,
    IReadOnlyList<InvoicePrintLine> Lines)
{
    public const string MessageName = "invoice-print-requested";
}

public sealed record InvoicePrintLine(Guid ProductId, int Quantity);
