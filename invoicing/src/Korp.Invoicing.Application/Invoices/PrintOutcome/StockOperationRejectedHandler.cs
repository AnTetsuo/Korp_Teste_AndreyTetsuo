using Application.Messaging.Contracts;
using Domain.Common;
using Domain.Invoices;
using Domain.Invoices.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Invoices.PrintOutcome;

/// <summary>
/// Returns the invoice to open with the reason stock gave, so the user can act on it and print
/// again. A reason longer than the column is truncated rather than rejected: a reply that cannot
/// be applied would leave the invoice printing forever.
/// </summary>
public sealed class StockOperationRejectedHandler(
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork,
    ILogger<StockOperationRejectedHandler> logger)
{
    public async Task HandleAsync(
        StockOperationRejected message,
        CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(message.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError(
                "Stock rejected the operation for invoice {InvoiceId}, which does not exist here.",
                message.InvoiceId);

            return;
        }

        var wasPrinting = invoice.Status == InvoiceStatus.Processing;

        var transition = invoice.FailPrinting(Fit(message.Reason));

        if (!transition.IsSuccess)
        {
            logger.LogWarning(
                "Ignored the stock rejection for invoice {InvoiceId}: {Reason}",
                message.InvoiceId,
                transition.ErrorMessage);

            return;
        }

        if (!wasPrinting)
        {
            logger.LogInformation(
                "Invoice {InvoiceId} is no longer printing; the repeated rejection changed nothing.",
                message.InvoiceId);

            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reopened invoice {InvoiceId} after stock rejected the operation: {Reason}",
            message.InvoiceId,
            message.Reason);
    }

    private static string Fit(string? reason)
    {
        const string ellipsis = "...";

        if (string.IsNullOrWhiteSpace(reason))
            return "Stock rejected this invoice without giving a reason.";

        var trimmed = reason.Trim();

        return trimmed.Length <= Invoice.FailureReasonMaxLength
            ? trimmed
            : trimmed[..(Invoice.FailureReasonMaxLength - ellipsis.Length)] + ellipsis;
    }
}
