using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

internal sealed class StockDbContextFactory : IDesignTimeDbContextFactory<StockDbContext>
{
    public StockDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.Load();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Stock")
            ?? "Host=localhost;Port=5432;Database=stock;Username=korp;Search Path=stock";

        var options = new DbContextOptionsBuilder<StockDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new StockDbContext(options);
    }
}
