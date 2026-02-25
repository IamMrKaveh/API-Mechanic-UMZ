var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

ConfigureAuthentication(builder);
ConfigureServices(builder);

var app = builder.Build();

ValidateAutoMapperConfiguration(app);

app.UseApplicationMiddleware();
app.MapControllers();
app.Run();

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

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10485760;
    });

    builder.Services.Configure<SecurityHeadersOptions>(
        configuration.GetSection("SecurityHeaders"));

    builder.Services.Configure<SecuritySettings>(
        configuration.GetSection(SecuritySettings.SectionName));

    builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
    {
        client.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    ConfigureRedisAndDataProtection(builder);

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(configuration);
    builder.Services.AddCustomCors(configuration);
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