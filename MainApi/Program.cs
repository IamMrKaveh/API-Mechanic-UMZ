Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Ledka");

    var builder = WebApplication.CreateBuilder(args);

    ConfigureSerilog(builder);
    ConfigureAuthentication(builder);
    ConfigureServices(builder);

    var app = builder.Build();

    ValidateAutoMapperConfiguration(app);

    app.UseApplicationMiddleware();
    app.MapControllers();

    Log.Information("Ledka started successfully.");
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
    var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
    var issuer = builder.Configuration["Jwt:Issuer"];
    var audience = builder.Configuration["Jwt:Audience"];

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
        });
}

static void ConfigureServices(WebApplicationBuilder builder)
{
    var configuration = builder.Configuration;
    ConfigureControllersAndApi(builder);
    ConfigureOptions(builder, configuration);
    ConfigureHttpClients(builder);
    ConfigureRedisAndDataProtection(builder);
    builder.Services.Configure<LiaraStorageSettings>(
        configuration.GetSection("LiaraStorage"));
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(configuration);
    builder.Services.AddCustomCors(configuration);
}

static void ConfigureControllersAndApi(WebApplicationBuilder builder)
{
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
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

static void ConfigureHttpClients(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
    {
        client.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });
}

static void ConfigureRedisAndDataProtection(WebApplicationBuilder builder)
{
    var redisConn = builder.Configuration.GetConnectionString("Redis") ?? string.Empty;
    var redis = ConnectionMultiplexer.Connect(redisConn);

    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddDataProtection()
        .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys-");
}

static void ValidateAutoMapperConfiguration(WebApplication app)
{
    app.Services
       .GetRequiredService<IMapper>()
       .ConfigurationProvider
       .AssertConfigurationIsValid();
}