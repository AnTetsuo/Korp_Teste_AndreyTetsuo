using Domain.Product;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductCode)
            .IsRequired()
            .HasMaxLength(Product.ProductCodeMaxLength);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(Product.DescriptionMaxLength);

        builder.Property(p => p.Active).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.ProductCode).IsUnique();
    }
}
