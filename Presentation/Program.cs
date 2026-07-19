using Application.Common.DependencyInjection;
using Infrastructure.Common.DependencyInjection;
using Presentation.Common.Logging;
using Serilog.Exceptions;

var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
var logsErrorDirectory = Path.Combine(logsDirectory, "errors");

Directory.CreateDirectory(logsDirectory);
Directory.CreateDirectory(logsErrorDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile(
            $"appsettings.{builder.Environment.EnvironmentName}.json",
            optional: true,
            reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .MinimumLevel.Warning()
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Error)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Error)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Error)

            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", "Mechanic.Api")
            .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)

            .Filter.ByExcluding(logEvent =>
                logEvent.Properties.TryGetValue("RequestPath", out var path) &&
                IsIgnoredPath(path.ToString()))

            .WriteTo.Console(
                outputTemplate: "[{Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
                restrictedToMinimumLevel: LogEventLevel.Warning)

            .WriteTo.File(
                new NoTimestampCompactJsonFormatter(),
                path: Path.Combine(logsDirectory, "log-.json"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 50 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                shared: true,
                restrictedToMinimumLevel: LogEventLevel.Warning)

            .WriteTo.Logger(errorLogger => errorLogger
                .Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Error)
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .WriteTo.File(
                    new NoTimestampCompactJsonFormatter(),
                    path: Path.Combine(logsErrorDirectory, "errors-.json"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    shared: true));
    });

    builder.AddApplicationAuthentication();

    builder.Services.AddPresentation(builder.Configuration, builder.Environment);
    builder.Services.AddApplicationServices();
    builder.Services.AddWalletTransferOptions(builder.Configuration);
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.ValidateRequiredConfiguration();

    var app = builder.Build();

    app.UseApplication();
    app.MapControllers();

    app
        .MapGet("/", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static bool IsIgnoredPath(string? path)
{
    if (string.IsNullOrEmpty(path))
        return false;

    return path.Contains("/health", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/swagger", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/favicon.ico", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/metrics", StringComparison.OrdinalIgnoreCase);
}
