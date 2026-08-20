using Application.Invoices.ListInvoices;
using Application.Messaging;
using Application.Messaging.Contracts;
using Domain.Common;
using Domain.Invoices;
using Infrastructure.Messaging;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Queries;
using Infrastructure.Persistence.Repositories;
using JasperFx;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        var connectionString = configuration.GetConnectionString("Invoicing")
            ?? throw new InvalidOperationException(
                "Connection string 'Invoicing' is not configured. Set it via user secrets or " +
                "the ConnectionStrings__Invoicing environment variable.");

        services.AddDbContextWithWolverineIntegration<InvoicingDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<InvoicingDbContext>());

        services.AddHealthChecks()
            .AddDbContextCheck<InvoicingDbContext>("database", tags: ["ready"]);

        services.AddScoped<IInvoiceRepository, InvoiceRepository>();

        services.AddScoped<IInvoiceReadRepository, InvoiceReadRepository>();

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

            options.PublishMessage<InvoicePrintRequested>()
                .ToRabbitQueue(MessagingConstants.StockOperationQueue);

            options.Policies.UseDurableOutboxOnAllSendingEndpoints();
        });

        services.AddScoped<IOutbox, WolverineOutbox>();

        return services;
    }
}
