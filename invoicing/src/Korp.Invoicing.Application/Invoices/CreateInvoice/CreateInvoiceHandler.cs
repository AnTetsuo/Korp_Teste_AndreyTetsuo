using Application.Common;
using Domain.Common;
using Domain.Invoices;
using Domain.Invoices.Items;

namespace Application.Invoices.CreateInvoice;

public sealed class CreateInvoiceHandler(
    IInvoiceRepository invoices,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CreateInvoiceCommand, Result<CreateInvoiceResponse>>
{
    public async Task<Result<CreateInvoiceResponse>> HandleAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var number = await invoices.NextNumberAsync(cancellationToken);

        var items = command.Items
            .Select(item => new InvoiceItemDto(
                item.ProductId,
                item.ProductCode,
                item.Description,
                item.Quantity))
            .ToList();

        var invoiceResult = Invoice.Open(number, items);
        
        if (!invoiceResult.IsSuccess)
            return Result<CreateInvoiceResponse>.Invalid([.. invoiceResult.ValidationErrors]);

        var invoice = invoiceResult.Value;

        invoices.Add(invoice);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateInvoiceResponse>.Created(new CreateInvoiceResponse(
            invoice.Id,
            invoice.Number,
            invoice.Status.ToString(),
            invoice.CreatedAt,
            [.. invoice.Items.Select(item => new InvoiceLine(
                item.ProductId,
                item.ProductCode,
                item.Description,
                item.Quantity))]));
    }
}
