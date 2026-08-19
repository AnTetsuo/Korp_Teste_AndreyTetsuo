using Application.Products.ListProducts;
using Domain.Common;
using Domain.Product;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Queries;
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
        var connectionString = configuration.GetConnectionString("Stock")
            ?? throw new InvalidOperationException(
                "Connection string 'Stock' is not configured. Set it via user secrets or " +
                "the ConnectionStrings__Stock environment variable.");

        services.AddDbContext<StockDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<StockDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<StockDbContext>("database", tags: ["ready"]);

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IEntityReferenceRepository, EntityReferenceRepository>();

        services.AddScoped<IProductReadRepository, ProductReadRepository>();

        return services;
    }
}
