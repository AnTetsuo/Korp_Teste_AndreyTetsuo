using Domain.Stocks.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions", t =>
            t.HasCheckConstraint("ck_transactions_quantity_positive", "quantity > 0"));

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TransactionType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Quantity).IsRequired();

        builder.Ignore(t => t.SignedQuantity);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => new { t.StockId, t.CreatedAt });

        builder.HasOne<EntityReference>()
            .WithMany()
            .HasForeignKey(t => t.ReferenceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
