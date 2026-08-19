using Application.Invoices.CreateInvoice;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateInvoiceHandler>();

        return services;
    }
}
