using Domain.Common;
using Domain.Invoices;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Invoicing")
            ?? throw new InvalidOperationException(
                "Connection string 'Invoicing' is not configured. Set it via user secrets or " +
                "the ConnectionStrings__Invoicing environment variable.");

        services.AddDbContext<InvoicingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InvoicingDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<InvoicingDbContext>("database", tags: ["ready"]);

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        return services;
    }
}
