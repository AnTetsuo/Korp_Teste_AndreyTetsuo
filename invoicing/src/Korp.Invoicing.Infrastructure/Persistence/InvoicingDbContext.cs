using System.Reflection;
using Domain.Common;
using Domain.Invoices;
using Domain.Invoices.Items;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class InvoicingDbContext(DbContextOptions<InvoicingDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public const string Schema = "invoicing";
    public const string InvoiceNumberSequence = "invoice_number_seq";
    public const string QualifiedInvoiceNumberSequence = $"{Schema}.{InvoiceNumberSequence}";

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>(InvoiceNumberSequence).StartsAt(1).IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
