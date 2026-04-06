using Application.Media.Features.Shared;
using Infrastructure;
using Infrastructure.Security.Settings;
using MainApi.Common.Extensions;
using MainApi.Common.Filters;
using MainApi.Common.Options;
using MainApi.Security.Settings;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    ConfigureSerilog(builder);
    ConfigureAuthentication(builder);
    ConfigureServices(builder);
    builder.ValidateRequiredConfiguration();

    var app = builder.Build();

    app.UseApplicationMiddleware();
    app.MapControllers();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Ledka terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureSerilog(WebApplicationBuilder builder)
{
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
            .Enrich.WithThreadName()
            .Enrich.WithProperty("Application", "Ledka"));
}

static void ConfigureAuthentication(WebApplicationBuilder builder)
{
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
    builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection(GoogleAuthSettings.SectionName));

    var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        })
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
        });
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    ConfigureControllersAndApi(builder);
    ConfigureOptions(builder, configuration);
    ConfigureRedisAndDataProtection(builder);

    builder.Services.Configure<LiaraStorageSettings>(
        configuration.GetSection("LiaraStorage"));

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(configuration);
    builder.Services.AddCustomCors(configuration);
}

static void ConfigureControllersAndApi(WebApplicationBuilder builder)
{
    builder.Services.AddCustomApiVersioning();

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
}

static void ConfigureOptions(WebApplicationBuilder builder, IConfiguration configuration)
{
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10485760;
    });

    builder.Services.Configure<SecurityHeadersOptions>(
        configuration.GetSection("SecurityHeaders"));

    builder.Services.Configure<SecuritySettings>(
        configuration.GetSection(SecuritySettings.SectionName));
}

static void ConfigureRedisAndDataProtection(WebApplicationBuilder builder)
{
    var redisConn = builder.Configuration.GetConnectionString("Redis") ?? string.Empty;
    var redis = ConnectionMultiplexer.Connect(redisConn);

    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys-");
}