using Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number).IsRequired();

        builder.Property(i => i.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.UpdatedAt).IsRequired();

        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.Status);

        builder.HasMany(i => i.Items)
            .WithOne()
            .HasForeignKey(item => item.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property<uint>("xmin")
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}
