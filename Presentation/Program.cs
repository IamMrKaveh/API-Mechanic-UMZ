using Application.Common.Interfaces;
using Application.Media.Features.Shared;
using Infrastructure.DependencyInjection;
using Infrastructure.Security.Settings;
using Presentation.Common.Options;
using Presentation.Common.Services;
using Presentation.Common.Swagger;
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

    ConfigureSerilog(builder);
    ConfigureAuthentication(builder);
    ConfigureServices(builder);

    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<OtpRateLimitFilter>();

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

static void ConfigureSerilog(WebApplicationBuilder builder)
{
    var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
    var logsErrorDir = Path.Combine(logsDir, "errors");

    Directory.CreateDirectory(logsDir);
    Directory.CreateDirectory(logsErrorDir);

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
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services.Configure<JwtSettings>(
        builder.Configuration.GetSection(JwtSettings.SectionName));

    builder.Services.Configure<GoogleAuthSettings>(
        builder.Configuration.GetSection(GoogleAuthSettings.SectionName));

    var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];

    var googleClientId =
        builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;

    var googleClientSecret =
        builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;

    var authBuilder = builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme =
                JwtBearerDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey)),

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

    if (!string.IsNullOrWhiteSpace(googleClientId) &&
        !string.IsNullOrWhiteSpace(googleClientSecret))
    {
        authBuilder.AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
        });
    }
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;

    ConfigureControllersAndApi(builder);
    ConfigureOptions(builder, configuration);

    builder.Services.Configure<LiaraStorageSettings>(
        configuration.GetSection("LiaraStorage"));

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructure(configuration);
    builder.Services.AddCustomCors(configuration);
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
        options.EnableAnnotations();

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