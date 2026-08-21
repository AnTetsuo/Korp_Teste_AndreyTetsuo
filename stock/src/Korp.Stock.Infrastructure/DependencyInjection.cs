using Application.Products.ListProducts;
using Application.Messaging;
using Application.Messaging.Contracts;
using Application.Stocks.Operations;
using Domain.Common;
using Domain.Product;
using Domain.Stocks;
using Domain.Stocks.Transactions;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Queries;
using Infrastructure.Persistence.Repositories;
using JasperFx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using JasperFx.CodeGeneration.Model;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

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

        services.AddDbContextWithWolverineIntegration<StockDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<StockDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<StockDbContext>("database", tags: ["ready"]);

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IEntityReferenceRepository, EntityReferenceRepository>();

        services.AddScoped<IProductReadRepository, ProductReadRepository>();
        services.AddScoped<IStockOperationReadRepository, StockOperationReadRepository>();

        services.AddMessaging(configuration, connectionString);

        return services;
    }

    private static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string connectionString)
    {
        var rabbit = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? new RabbitMqOptions();

        services.AddWolverine(options =>
        {
            options.UseRuntimeCompilation();

            options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

            options.PersistMessagesWithPostgresql(
                connectionString, MessagingConstants.MessageStoreSchema);

            options.AutoBuildMessageStorageOnStartup = AutoCreate.None;

            options.UseEntityFrameworkCoreTransactions();

            options.UseRabbitMq(factory =>
                {
                    factory.HostName = rabbit.Host;
                    factory.Port = rabbit.Port;
                    factory.UserName = rabbit.User;
                    factory.Password = rabbit.Password;
                })
                .AutoProvision();

            options.Discovery.IncludeType<InvoicePrintRequestedHandler>();

            options.ListenToRabbitQueue(MessagingConstants.OperationQueue)
                .UseDurableInbox();

            options.PublishMessage<StockOperationApplied>()
                .ToRabbitQueue(MessagingConstants.RepliesQueue);

            options.PublishMessage<StockOperationRejected>()
                .ToRabbitQueue(MessagingConstants.RepliesQueue);

            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        });

        services.AddScoped<IOutbox, WolverineOutbox>();

        return services;
    }
}
