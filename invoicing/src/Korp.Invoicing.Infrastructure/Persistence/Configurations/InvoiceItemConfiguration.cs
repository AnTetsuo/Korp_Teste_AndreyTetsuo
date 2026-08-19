using Domain.Invoices.Items;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("invoice_items", t =>
            t.HasCheckConstraint("ck_invoice_items_quantity_positive", "quantity > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductId).IsRequired();

        builder.Property(i => i.ProductCode)
            .IsRequired()
            .HasMaxLength(InvoiceItem.ProductCodeMaxLength);

        builder.Property(i => i.Description)
            .IsRequired()
            .HasMaxLength(InvoiceItem.DescriptionMaxLength);

        builder.Property(i => i.Quantity).IsRequired();

        builder.HasIndex(i => new { i.InvoiceId, i.ProductId }).IsUnique();
    }
}
