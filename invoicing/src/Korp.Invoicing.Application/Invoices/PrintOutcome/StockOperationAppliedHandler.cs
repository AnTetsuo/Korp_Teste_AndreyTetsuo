using Application.Messaging.Contracts;
using Domain.Common;
using Domain.Invoices;
using Domain.Invoices.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Invoices.PrintOutcome;

/// <summary>
/// Closes the invoice that stock confirmed. Stock replies at least once, so this must stay a
/// no-op on redelivery — <see cref="Invoice.Close"/> is idempotent for exactly that reason.
/// </summary>
public sealed class StockOperationAppliedHandler(
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork,
    ILogger<StockOperationAppliedHandler> logger)
{
    public async Task HandleAsync(
        StockOperationApplied message,
        CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(message.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError(
                "Stock confirmed the operation for invoice {InvoiceId}, which does not exist here.",
                message.InvoiceId);

            return;
        }

        var wasAlreadyClosed = invoice.Status == InvoiceStatus.Closed;
        var wasReopened = invoice.Status == InvoiceStatus.Open;

        var transition = invoice.Close();

        if (!transition.IsSuccess)
        {
            logger.LogWarning(
                "Ignored the stock confirmation for invoice {InvoiceId}: {Reason}",
                message.InvoiceId,
                transition.ErrorMessage);

            return;
        }

        if (wasAlreadyClosed)
        {
            logger.LogInformation(
                "Invoice {InvoiceId} is already closed; the repeated confirmation changed nothing.",
                message.InvoiceId);

            return;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (wasReopened)
        {
            logger.LogInformation(
                "Closed invoice {InvoiceId} on a late confirmation; it had been reopened after an "
                + "attempt that never came back.",
                message.InvoiceId);

            return;
        }

        logger.LogInformation(
            "Closed invoice {InvoiceId} after stock confirmed the operation.",
            message.InvoiceId);
    }
}
