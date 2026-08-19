using Application.Invoices.CreateInvoice;
using Application.Invoices.ListInvoices;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateInvoiceHandler>();
        services.AddScoped<ListInvoicesHandler>();

        return services;
    }
}
