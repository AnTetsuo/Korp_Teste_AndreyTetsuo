using Domain.Stocks.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class EntityReferenceConfiguration : IEntityTypeConfiguration<EntityReference>
{
    public void Configure(EntityTypeBuilder<EntityReference> builder)
    {
        builder.ToTable("entity_references");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ReferenceId).IsRequired();

        builder.Property(e => e.EntityType)
            .HasConversion<int>()
            .IsRequired();
    }
}
