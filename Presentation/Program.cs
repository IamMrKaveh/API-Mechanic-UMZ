using Application.Auth.Contracts;
using Application.Common.Interfaces;
using Application.Media.Features.Shared;
using Infrastructure.DependencyInjection;
using Presentation.Common.Options;
using Presentation.Common.Services;
using Presentation.Common.Swagger;
using Presentation.Security;
using Presentation.Security.Services;
using Presentation.Security.Settings;

var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
var logsErrorDirectory = Path.Combine(logsDirectory, "errors");

Directory.CreateDirectory(logsDirectory);
Directory.CreateDirectory(logsErrorDirectory);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine(logsDirectory, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
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

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithProcessName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Presentation"));

    builder.AddApplicationAuthentication();

    var configuration = builder.Configuration;

    ConfigureControllersAndApi(builder);
    ConfigureOptions(builder, configuration);

    builder.Services.Configure<LiaraStorageSettings>(configuration.GetSection("LiaraStorage"));
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<OtpRateLimitFilter>();
    builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructure(configuration);
    builder.Services.AddCustomCors(configuration);

    builder.Services.AddScoped<IMapper, ServiceMapper>();

    builder.ValidateRequiredConfiguration();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                $"Ledka {description.GroupName.ToUpperInvariant()}");
        }
        options.RoutePrefix = "swagger";
    });

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseApplicationMiddleware();
    app.MapControllers();
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

static void ConfigureControllersAndApi(WebApplicationBuilder builder)
{
    builder.Services.AddCustomApiVersioning();

    builder.Services.AddControllers(options =>
    {
        options.Filters.AddService<OtpRateLimitFilter>();
        options.Filters.Add<ValidationFilter>();
    });

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.SuppressModelStateInvalidFilter = true;
    });

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddTransient<
        IConfigureOptions<SwaggerGenOptions>,
        SwaggerConfigureOptions>();

    builder.Services.AddSwaggerGen(options =>
    {
        options.OperationFilter<RemoveVersionParameterOperationFilter>();
        options.OperationFilter<DefaultResponseOperationFilter>();

        options.AddSecurityDefinition("Bearer",
            new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

        options.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
            });
    });

    builder.Services.AddApplicationPipelines();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
    });
}

static void ConfigureOptions(
    WebApplicationBuilder builder,
    IConfiguration configuration)
{
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
    });

    builder.Services.Configure<SecurityHeadersOptions>(
        configuration.GetSection("SecurityHeaders"));

    builder.Services.Configure<SecuritySettings>(
        configuration.GetSection(SecuritySettings.SectionName));
}