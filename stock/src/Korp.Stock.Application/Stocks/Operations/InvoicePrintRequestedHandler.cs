using Application.Messaging;
using Application.Messaging.Contracts;
using Domain.Common;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Domain.Stocks.Transactions.Enums;
using Microsoft.Extensions.Logging;

namespace Application.Stocks.Operations;

public sealed class InvoicePrintRequestedHandler(
    IStockRepository stocks,
    IEntityReferenceRepository references,
    IStockOperationReadRepository operations,
    IUnitOfWork unitOfWork,
    IOutbox outbox,
    ILogger<InvoicePrintRequestedHandler> logger)
{
    internal const int MaxAttempts = 3;

    public async Task HandleAsync(
        InvoicePrintRequested message,
        CancellationToken cancellationToken)
    {
        var shape = ValidateShape(message);

        if (!shape.IsSuccess)
        {
            await ReplyAsync(Reject(message.InvoiceId, shape), cancellationToken);

            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var result = await AttemptAsync(message, cancellationToken);

                if (!result.IsSuccess)
                {
                    await ReplyAsync(Reject(message.InvoiceId, result), cancellationToken);

                    return;
                }

                logger.LogInformation(
                    "Applied the stock operation for invoice {InvoiceId} across {LineCount} lines.",
                    message.InvoiceId,
                    result.Value.Lines.Count);

                return;
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                unitOfWork.DiscardChanges();
            }
            catch (UniqueConstraintViolationException)
            {
                logger.LogInformation(
                    "Invoice {InvoiceId} was already applied by a concurrent handler.",
                    message.InvoiceId);

                await ReplyAsync(new StockOperationApplied(message.InvoiceId), cancellationToken);

                return;
            }
        }
    }
    
    private async Task ReplyAsync<TReply>(TReply reply, CancellationToken cancellationToken)
        where TReply : notnull
    {
        unitOfWork.DiscardChanges();

        await outbox.PublishAsync(reply, cancellationToken);
        await outbox.SaveChangesAndFlushAsync(cancellationToken);
    }

    private async Task<Result<OperationResponse>> AttemptAsync(
        InvoicePrintRequested message,
        CancellationToken cancellationToken)
    {
        var existing = await references.GetAsync(
            EntityType.Invoice, message.InvoiceId, cancellationToken);

        if (existing is not null)
        {
            var applied = await operations.GetByInvoiceIdAsync(message.InvoiceId, cancellationToken);

            if (applied is null)
                return Result<OperationResponse>.Error(
                    $"Invoice '{message.InvoiceId}' is bound to an operation that recorded no movements.");

            await ReplyAsync(new StockOperationApplied(message.InvoiceId), cancellationToken);

            return Result<OperationResponse>.Success(applied);
        }

        var loaded = await stocks.GetByProductIdsAsync(
            [.. message.Lines.Select(line => line.ProductId)], cancellationToken);

        var byProduct = loaded.ToDictionary(stock => stock.ProductId);

        var failures = new List<ValidationError>();

        foreach (var line in message.Lines)
        {
            if (!byProduct.TryGetValue(line.ProductId, out var stock))
            {
                failures.Add(new ValidationError(
                    line.ProductId.ToString(), "No stock is registered for this product."));

                continue;
            }

            if (stock.Quantity < line.Quantity)
                failures.Add(new ValidationError(
                    line.ProductId.ToString(),
                    $"Insufficient balance: {stock.Quantity} available, {line.Quantity} requested."));
        }

        if (failures.Count > 0)
            return Result<OperationResponse>.Conflict(
                "Stock cannot satisfy every line of this invoice.", [.. failures]);

        var referenceResult = EntityReference.BindReference(EntityType.Invoice, message.InvoiceId);

        if (!referenceResult.IsSuccess)
            return Result<OperationResponse>.Invalid([.. referenceResult.ValidationErrors]);

        var reference = referenceResult.Value;

        references.Add(reference);

        var lines = new List<OperationLine>(message.Lines.Count);

        foreach (var line in message.Lines)
        {
            var stock = byProduct[line.ProductId];

            var movement = stock.Operate(line.Quantity, reference.Id);

            if (!movement.IsSuccess)
                return Result<OperationResponse>.Conflict(
                    "Stock cannot satisfy every line of this invoice.",
                    new ValidationError(
                        line.ProductId.ToString(),
                        movement.ErrorMessage ?? "The line was rejected."));

            lines.Add(new OperationLine(line.ProductId, line.Quantity, stock.Quantity));
        }

        await outbox.PublishAsync(new StockOperationApplied(message.InvoiceId), cancellationToken);
        await outbox.SaveChangesAndFlushAsync(cancellationToken);

        return new OperationResponse(message.InvoiceId, lines);
    }

    private StockOperationRejected Reject(Guid invoiceId, Result result)
    {
        if (result.Status is not (ResultStatus.Conflict or ResultStatus.Invalid))
            throw new InvalidOperationException(
                $"The stock operation for invoice '{invoiceId}' failed: {result.ErrorMessage}");

        var reason = Describe(result);

        logger.LogWarning(
            "Rejected the stock operation for invoice {InvoiceId}: {Reason}",
            invoiceId,
            reason);

        return new StockOperationRejected(invoiceId, reason);
    }

    private static string Describe(Result result)
    {
        var headline = result.ErrorMessage ?? "The stock operation was rejected.";

        if (result.ValidationErrors.Count == 0)
            return headline;

        var details = string.Join(
            " ", result.ValidationErrors.Select(e => $"{e.Field}: {e.Message}"));

        return $"{headline} {details}";
    }

    private static Result ValidateShape(InvoicePrintRequested message)
    {
        var errors = new ValidationErrors()
            .RequireId(message.InvoiceId, "invoiceId", "Invoice id");

        if (message.Lines is null or { Count: 0 })
            return Result.Invalid(errors.Add("lines", "At least one line is required.").ToArray());

        for (var index = 0; index < message.Lines.Count; index++)
            errors
                .RequireId(message.Lines[index].ProductId, $"lines[{index}].productId", "Product id")
                .RequirePositive(message.Lines[index].Quantity, $"lines[{index}].quantity", "Quantity");

        foreach (var duplicate in message.Lines.GroupBy(line => line.ProductId).Where(g => g.Count() > 1))
            errors.Add("lines", $"Product '{duplicate.Key}' appears more than once; merge the lines.");

        return errors.Any ? Result.Invalid(errors.ToArray()) : Result.Success();
    }
}
