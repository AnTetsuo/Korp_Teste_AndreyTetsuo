using Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

internal sealed class InvoicingDbContextFactory : IDesignTimeDbContextFactory<InvoicingDbContext>
{
    public InvoicingDbContext CreateDbContext(string[] args)
    {
        EnvironmentFile.Load();

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Invoicing")
            ?? "Host=localhost;Port=5432;Database=invoicing;Username=korp;Search Path=invoicing";

        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new InvoicingDbContext(options);
    }
}
