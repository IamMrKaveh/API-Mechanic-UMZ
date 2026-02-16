using Infrastructure.Search.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// -----------------------------
// DB Context
// -----------------------------
string dbConn = builder.Configuration.GetConnectionString("PoolerConnection")
    ?? throw new InvalidOperationException("Database connection string is not configured.");
builder.Services.AddDbContext<LedkaContext>(options =>
    options.UseNpgsql(dbConn,
        npgsql => npgsql.EnableRetryOnFailure())
);

var jwtKey = builder.Configuration["Jwt:Key"];
var issuer = builder.Configuration["Jwt:Issuer"];
var audience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
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

// 2. Services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");

builder.Services.AddHttpClient<ILocationService, LocationService>(client =>
{
    client.BaseAddress = new Uri("https://iran-locations-api.ir/api/v1/fa/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHttpClient<IPaymentGateway, ZarinPalPaymentGateway>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});

string redisConn = builder.Configuration.GetConnectionString("Redis")
    ?? "";
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConn));

var redis = ConnectionMultiplexer.Connect(redisConn);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys-");

// 3. Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddApplicationPipelines(); // Validation, Transaction Behaviors
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck<ElasticsearchDLQHealthCheck>("elasticsearch_dlq")
    .AddNpgSql(dbConn, name: "postgresql");

// 4. Configuration Options
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10485760; // 10MB
});

builder.Services.Configure<SecurityHeadersOptions>(
    builder.Configuration.GetSection("SecurityHeaders"));

// 5. CORS
builder.Services.AddCustomCors(builder.Configuration);

var app = builder.Build();

app.Services
   .GetRequiredService<IMapper>()
   .ConfigurationProvider
   .AssertConfigurationIsValid();

// 6. Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Production Security Headers
    app.UseSecurityMiddleware();
}

// Global Exception Handler (Must be early)
app.UseCustomExceptionHandler();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCustomCors();

// Rate Limiting (Global)
app.UseMiddleware<RateLimitMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Correlation ID for logging
app.UseMiddleware<CorrelationIdMiddleware>();

// Admin Security
app.UseAdminIpWhitelist();

// Webhook Security
app.UseMiddleware<WebhookIpWhitelistMiddleware>();

app.MapControllers();

app.Run();