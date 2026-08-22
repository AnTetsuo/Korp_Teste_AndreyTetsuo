using System.Text.Json;
using Domain.Invoices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        builder.Property(i => i.FailureReason)
            .HasMaxLength(Invoice.FailureReasonMaxLength);

        builder.Property(i => i.FailureCode)
            .HasMaxLength(Invoice.FailureCodeMaxLength);

        builder.Property(i => i.FailureLines)
            .HasColumnType("jsonb")
            .HasConversion(
                lines => JsonSerializer.Serialize(lines, JsonOptions),
                json => JsonSerializer.Deserialize<List<InvoiceFailureLine>>(json, JsonOptions)
                        ?? new List<InvoiceFailureLine>(),
                new ValueComparer<IReadOnlyList<InvoiceFailureLine>>(
                    (left, right) => left!.SequenceEqual(right!),
                    lines => lines.Aggregate(0, (hash, line) => HashCode.Combine(hash, line)),
                    lines => lines.ToList()));

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
