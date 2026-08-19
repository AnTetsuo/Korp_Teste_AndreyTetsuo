using Api.Endpoints.Invoices;
using Api.Endpoints.Invoices.Create;
using Application;
using FluentValidation;
using Infrastructure;
using Infrastructure.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

EnvironmentFile.Load();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Korp Invoicing API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    builder.Services.AddOpenApi();

    builder.Services.AddProblemDetails();

    builder.Services.Configure<RouteHandlerOptions>(o => o.ThrowOnBadRequest = false);

    builder.Services.AddValidatorsFromAssemblyContaining<CreateInvoiceRequestValidator>();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    app.UseExceptionHandler();

    app.UseStatusCodePages();

    app.UseSerilogRequestLogging(options =>
        options.GetLevel = (httpContext, _, exception) =>
            exception is not null
                ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health")
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Information);

    if (builder.Configuration.GetValue("ApiDocs:Enabled", app.Environment.IsDevelopment()))
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

    app.MapInvoiceEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Korp Invoicing API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
