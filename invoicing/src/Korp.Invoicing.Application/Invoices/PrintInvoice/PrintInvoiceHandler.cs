using Application.Common;
using Application.Invoices.PrintOutcome;
using Application.Messaging;
using Application.Messaging.Contracts;
using Domain.Common;
using Domain.Invoices;

namespace Application.Invoices.PrintInvoice;

public sealed class PrintInvoiceHandler(
    IInvoiceRepository invoices,
    IOutbox outbox)
    : ICommandHandler<PrintInvoiceCommand, Result<PrintInvoiceResponse>>
{
    public async Task<Result<PrintInvoiceResponse>> HandleAsync(
        PrintInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var invoice = await invoices.GetByIdAsync(command.InvoiceId, cancellationToken);

        if (invoice is null)
            return Result<PrintInvoiceResponse>.NotFound(
                $"Invoice '{command.InvoiceId}' was not found.");

        var transition = invoice.BeginPrinting();

        if (!transition.IsSuccess)
            return Result<PrintInvoiceResponse>.Conflict(
                transition.ErrorMessage ?? "This invoice cannot be printed.");

        var message = new InvoicePrintRequested(
            invoice.Id,
            [.. invoice.Items.Select(item => new InvoicePrintLine(item.ProductId, item.Quantity))]);

        await outbox.PublishAsync(message, cancellationToken);

        await outbox.ScheduleAsync(
            new PrintTimeoutCheck(invoice.Id, 1),
            PrintTimeoutCheckHandler.FirstDelay,
            cancellationToken);

        try
        {
            await outbox.SaveChangesAndFlushAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return Result<PrintInvoiceResponse>.Conflict(
                $"Invoice '{command.InvoiceId}' is already being printed.");
        }

        return Result<PrintInvoiceResponse>.Success(new PrintInvoiceResponse(
            invoice.Id,
            invoice.Number,
            invoice.Status.ToString(),
            invoice.UpdatedAt));
    }
}
