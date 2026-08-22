namespace Domain.Invoices;

public sealed record InvoiceFailureLine(Guid ProductId, int Requested, int Available);
