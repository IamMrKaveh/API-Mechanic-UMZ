var builder = WebApplication.CreateBuilder(args);

// 1. Logging
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

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

// 3. Clean Architecture Layers
builder.Services.AddApplicationServices();
builder.Services.AddApplicationPipelines(); // Validation, Transaction Behaviors
builder.Services.AddInfrastructureServices(builder.Configuration);

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