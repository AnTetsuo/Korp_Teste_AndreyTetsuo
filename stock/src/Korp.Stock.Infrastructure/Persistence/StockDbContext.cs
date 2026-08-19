using System.Reflection;
using Domain.Common;
using Domain.Product;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class StockDbContext(DbContextOptions<StockDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<EntityReference> EntityReferences => Set<EntityReference>();

    public const string Schema = "stock";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
