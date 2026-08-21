using Application.Messaging;
using Application.Messaging.Contracts;
using Domain.Invoices;
using Domain.Invoices.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Invoices.PrintOutcome;

public sealed class PrintTimeoutCheckHandler(
    IInvoiceRepository invoices,
    IOutbox outbox,
    ILogger<PrintTimeoutCheckHandler> logger)
{
    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
        TimeSpan.FromSeconds(120)
    ];

    public const string GaveUpReason =
        "Stock service did not confirm the consumption; try printing again.";

    public static TimeSpan FirstDelay => Backoff[0];

    public async Task HandleAsync(PrintTimeoutCheck message, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetByIdAsync(message.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            logger.LogError(
                "Timeout check fired for invoice {InvoiceId}, which does not exist.",
                message.InvoiceId);

            return;
        }

        if (invoice.Status != InvoiceStatus.Processing)
        {
            logger.LogDebug(
                "Timeout check for invoice {InvoiceId} found it {Status}; nothing to do.",
                message.InvoiceId,
                invoice.Status);

            return;
        }

        if (message.Attempt < Backoff.Length)
        {
            await RetryAsync(invoice, message.Attempt, cancellationToken);

            return;
        }

        var transition = invoice.FailPrinting(GaveUpReason);

        if (!transition.IsSuccess)
        {
            logger.LogWarning(
                "Timeout check could not reopen invoice {InvoiceId}: {Reason}",
                message.InvoiceId,
                transition.ErrorMessage);

            return;
        }

        await outbox.SaveChangesAndFlushAsync(cancellationToken);

        logger.LogWarning(
            "Reopened invoice {InvoiceId} after {Attempts} attempts without a reply from stock.",
            message.InvoiceId,
            message.Attempt);
    }

    private async Task RetryAsync(Invoice invoice, int attempt, CancellationToken cancellationToken)
    {
        var request = new InvoicePrintRequested(
            invoice.Id,
            [.. invoice.Items.Select(item => new InvoicePrintLine(item.ProductId, item.Quantity))]);

        // Re-publishing is safe because stock keys on the invoice id: it either applies the
        // operation once or recognises it as already applied and replies applied again.
        await outbox.PublishAsync(request, cancellationToken);
        await outbox.ScheduleAsync(
            new PrintTimeoutCheck(invoice.Id, attempt + 1), Backoff[attempt], cancellationToken);
        await outbox.SaveChangesAndFlushAsync(cancellationToken);

        logger.LogWarning(
            "Invoice {InvoiceId} is still printing after attempt {Attempt}; asked stock again.",
            invoice.Id,
            attempt);
    }
}
