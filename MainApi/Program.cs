#region Serilog Configuration
// پیکربندی Serilog برای لاگ‌گیری از برنامه
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection", LogEventLevel.Error)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_000_000,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1),
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();
#endregion

try
{
    Log.Information("Starting API");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    #region Database Configuration
    // پیکربندی اتصال به دیتابیس PostgreSQL
    var connectionString = builder.Configuration.GetConnectionString("PoolerConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Log.Fatal("Database connection string 'PoolerConnection' is missing!");
        throw new InvalidOperationException("Connection string 'PoolerConnection' not found.");
    }

    Log.Information("[RUNTIME MODE] Database connection validated: {Connection}",
        MaskConnectionString(connectionString));

    // ثبت DbContext با Connection Pooling
    builder.Services.AddDbContextPool<LedkaContext>((serviceProvider, options) =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(300);
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
            npgsqlOptions.SetPostgresVersion(new Version(15, 0));
        });

        options.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    }, poolSize: 15);

    builder.Services.AddScoped<IUnitOfWork, Infrastructure.Persistence.UnitOfWork>();
    Log.Information("Database context registered successfully");
    #endregion

    #region Redis Configuration
    // پیکربندی Redis برای Caching و Data Protection
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        try
        {
            var redisOptions = ConfigurationOptions.Parse(redisConnectionString);
            redisOptions.Ssl = true;
            redisOptions.SslProtocols = SslProtocols.Tls12;
            redisOptions.AbortOnConnectFail = false;
            redisOptions.AllowAdmin = false;
            redisOptions.ConnectTimeout = 5000;
            redisOptions.SyncTimeout = 10000;
            redisOptions.KeepAlive = 60;

            var redisConnection = await ConnectionMultiplexer.ConnectAsync(redisOptions);
            builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
            builder.Services.AddSingleton(sp => redisConnection.GetDatabase());
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();

            Log.Information("Redis connected successfully. Using Redis for caching and Data Protection.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Redis connection failed, using in-memory services.");
            RegisterInMemoryServices(builder.Services);
        }
    }
    else
    {
        Log.Warning("Redis connection string not found. Using in-memory services.");
        RegisterInMemoryServices(builder.Services);
    }
    #endregion

    #region AutoMapper Configuration
    // ثبت AutoMapper برای Mapping بین Entities و DTOs
    builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
    #endregion

    #region Application Services Registration
    // ثبت سرویس‌های لایه Application
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<ICategoryGroupService, CategoryGroupService>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IOrderItemService, OrderItemService>();
    builder.Services.AddScoped<IOrderStatusService, OrderStatusService>();
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddScoped<IReviewService, ReviewService>();
    builder.Services.AddScoped<IDiscountService, DiscountService>();
    builder.Services.AddScoped<IInventoryService, InventoryService>();
    builder.Services.AddScoped<IMediaService, MediaService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddScoped<IAdminProductService, AdminProductService>();
    builder.Services.AddScoped<IAdminCategoryService, AdminCategoryService>();
    builder.Services.AddScoped<IAdminCategoryGroupService, AdminCategoryGroupService>();
    builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
    builder.Services.AddScoped<IAdminOrderStatusService, AdminOrderStatusService>();
    builder.Services.AddScoped<IAdminReviewService, AdminReviewService>();
    builder.Services.AddScoped<IAdminUserService, AdminUserService>();
    #endregion

    #region Infrastructure Services Registration
    // ثبت سرویس‌های لایه Infrastructure
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddSingleton<IStorageService, LiaraStorageService>();
    builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    #endregion

    #region Repositories Registration
    // ثبت Repository‌ها برای دسترسی به داده‌ها
    builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
    builder.Services.AddScoped<ICategoryGroupRepository, CategoryGroupRepository>();
    builder.Services.AddScoped<IProductRepository, ProductRepository>();
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
    builder.Services.AddScoped<ICartRepository, CartRepository>();
    builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();
    builder.Services.AddScoped<IMediaRepository, MediaRepository>();
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IAuditRepository, AuditRepository>();
    builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
    builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
    builder.Services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
    #endregion

    #region Current User Service
    // سرویس کاربر جاری برای دسترسی به اطلاعات کاربر لاگین شده
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    #endregion

    #region Configuration Options
    // تنظیمات مختلف برنامه از appsettings.json
    builder.Services.Configure<LiaraStorageSettings>(builder.Configuration.GetSection("LiaraStorage"));
    builder.Services.Configure<ZarinpalSettingsDto>(builder.Configuration.GetSection("Zarinpal"));
    builder.Services.Configure<SecurityHeadersOptions>(builder.Configuration.GetSection("SecurityHeaders"));
    builder.Services.Configure<FrontendUrlsDto>(builder.Configuration.GetSection("FrontendUrls"));

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    });

    Log.Information("Application services registered");
    #endregion

    #region Security Configuration
    // تنظیمات امنیتی برای محدودیت حجم درخواست‌ها
    var maxRequestSize = builder.Configuration.GetValue<long>("Security:MaxRequestSizeInBytes", 10_485_760);
    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = maxRequestSize;
    });
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxRequestSize;
        options.ValueLengthLimit = (int)maxRequestSize;
    });

    // پیکربندی Antiforgery برای جلوگیری از حملات CSRF
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "__Host-XSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
    });

    // پیکربندی HTML Sanitizer برای جلوگیری از XSS
    builder.Services.AddSingleton<IHtmlSanitizer>(_ => new HtmlSanitizer(new HtmlSanitizerOptions
    {
        AllowedTags = { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
        AllowedAttributes = { "class" },
        AllowedSchemes = { "http", "https" }
    }));

    builder.Services.AddHttpContextAccessor();
    #endregion

    #region Authorization Configuration
    // تنظیمات Authorization و Policy‌های دسترسی
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddIpWhitelist(builder.Configuration);
    #endregion

    #region JWT Authentication Configuration
    // پیکربندی احراز هویت JWT
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrEmpty(jwtKey))
    {
        Log.Fatal("JWT Key is not configured!");
        throw new InvalidOperationException("JWT Key not configured");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(15),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RequireExpirationTime = true,
            ValidateTokenReplay = true
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["jwt_token"];
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("Token-Expired", "true");
                }
                Log.Warning("Authentication failed: {Exception}", context.Exception.Message);
                return Task.CompletedTask;
            }
        };
    });

    Log.Information("Authentication configured");
    #endregion

    #region HTTP Clients Configuration
    // پیکربندی HTTP Clients با Retry و Circuit Breaker
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("External API retry {RetryCount} after {Timespan}s due to: {Result}",
                    retryCount, timespan.TotalSeconds, outcome.Result?.StatusCode);
            });

    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
                Log.Error("External API circuit breaker opened for {Duration}s", duration.TotalSeconds),
            onReset: () =>
                Log.Information("External API circuit breaker reset"));

    builder.Services.AddHttpClient<IZarinpalService, ZarinpalService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "MechanicAPI/1.0");
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

    builder.Services.AddHttpClient<ILocationService, LocationService>()
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy);

    Log.Information("HTTP clients configured");
    #endregion

    #region CORS Configuration
    // پیکربندی CORS برای دسترسی از دامنه‌های مجاز
    var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://ledka-co.ir", "https://www.ledka-co.ir" };

    if (builder.Environment.IsDevelopment())
    {
        allowedOrigins = allowedOrigins.Concat(new[] { "http://localhost:4200", "https://localhost:4200" }).ToArray();
    }

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SecurePolicy", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition", "X-Pagination", "Token-Expired", "X-Correlation-ID")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    Log.Information("CORS configured with origins: {Origins}", string.Join(", ", allowedOrigins));
    #endregion

    #region Output Cache Configuration
    // پیکربندی Output Cache برای بهبود عملکرد
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(policyBuilder =>
            policyBuilder.Expire(TimeSpan.FromMinutes(5)).SetVaryByHeader("Accept-Encoding"));

        options.AddPolicy("CacheForUser", policyBuilder =>
            policyBuilder.Expire(TimeSpan.FromMinutes(2))
                .SetVaryByHeader("Authorization")
                .Tag("cart_", "user_"));

        options.AddPolicy("LongCache", policyBuilder =>
            policyBuilder.Expire(TimeSpan.FromHours(1))
                .Tag("products", "categories"));
    });
    #endregion

    #region Controllers Configuration
    // پیکربندی Controllers و JSON Serialization
    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    Log.Information("Controllers and caching configured");
    #endregion

    #region Swagger Configuration
    // پیکربندی Swagger برای مستندسازی API
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Mechanic API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
    #endregion

    #region Health Checks Configuration
    // پیکربندی Health Checks برای مانیتورینگ
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString,
            name: "postgresql",
            timeout: TimeSpan.FromSeconds(5),
            tags: new[] { "db", "ready" });

    Log.Information("Health checks configured");
    #endregion

    #region Kestrel Configuration
    // پیکربندی Kestrel Web Server
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });

    builder.Host.ConfigureHostOptions(options =>
        options.ShutdownTimeout = TimeSpan.FromSeconds(30));

    Log.Information("Kestrel configured");
    #endregion

    #region Build Application
    var app = builder.Build();
    Log.Information("Application built successfully");
    #endregion

    #region Middleware Pipeline Configuration
    app.UseForwardedHeaders();

    // پیکربندی Middleware بر اساس محیط (Development/Production)
    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseHttpsRedirection();
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            c.RoutePrefix = "swagger";
        });

        // حذف Security Headers برای Swagger
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.Response.Headers.Remove("Content-Security-Policy");
                context.Response.Headers.Remove("X-Frame-Options");
                context.Response.Headers.Remove("X-Content-Type-Options");
            }
            await next();
        });

        Log.Information("Development middleware enabled");
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
        Log.Information("Production middleware enabled");
    }

    // Global Exception Handler
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionFeature = context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

            if (exceptionFeature?.Error != null)
            {
                Log.Error(exceptionFeature.Error,
                    "Unhandled exception for path {Path}", exceptionFeature.Path);
            }

            var errorResponse = new
            {
                error = "An unexpected error occurred. Please try again later.",
                traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        });
    });

    app.UseStaticFiles();
    app.UseCors("SecurePolicy");
    app.UseRouting();
    app.UseSecurityMiddleware();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    app.UseOutputCache();

    Log.Information("Middleware pipeline configured");
    #endregion

    #region Endpoint Mapping
    app.MapControllers();

    // Health Check Endpoints
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds,
                    description = e.Value.Description
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            }, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(result);
        },
        AllowCachingResponses = false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        AllowCachingResponses = false
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        AllowCachingResponses = false
    });

    Log.Information("Endpoints mapped successfully");
    #endregion

    #region Application Lifetime Events
    // رویدادهای چرخه حیات برنامه
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

    lifetime.ApplicationStarted.Register(() =>
        Log.Information("API started successfully in {Environment} mode.",
            app.Environment.EnvironmentName));

    lifetime.ApplicationStopping.Register(() =>
        Log.Information("Application is shutting down gracefully..."));

    lifetime.ApplicationStopped.Register(() =>
    {
        var redis = app.Services.GetService<IConnectionMultiplexer>();
        redis?.Dispose();
        Log.Information("Application stopped.");
    });
    #endregion

    Log.Information("Starting web server...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.Information("Flushing logs...");
    await Log.CloseAndFlushAsync();
}

#region Helper Methods

/// <summary>
/// مخفی کردن پسورد در Connection String برای لاگ امن
/// </summary>
static string MaskConnectionString(string connectionString)
{
    var parts = connectionString.Split(';');
    for (int i = 0; i < parts.Length; i++)
    {
        var part = parts[i].Trim();
        if (part.StartsWith("Password=", StringComparison.OrdinalIgnoreCase) ||
            part.StartsWith("Pwd=", StringComparison.OrdinalIgnoreCase))
        {
            var keyValue = part.Split('=');
            if (keyValue.Length == 2)
            {
                parts[i] = $"{keyValue[0]}=***HIDDEN***";
            }
        }
    }
    return string.Join(";", parts);
}

/// <summary>
/// ثبت سرویس‌های In-Memory به جای Redis
/// </summary>
static void RegisterInMemoryServices(IServiceCollection services)
{
    services.AddSingleton<IMemoryCache, MemoryCache>();
    services.AddSingleton<ICacheService, InMemoryCacheService>();
    services.AddOutputCache();

    var dataProtectionBuilder = services.AddDataProtection()
        .SetApplicationName("MechanicAPI")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

    var keysPath = "/tmp/dataprotection-keys";
    try
    {
        if (!Directory.Exists(keysPath))
        {
            Directory.CreateDirectory(keysPath);
        }
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        Log.Information("Data Protection using file system at: {Path}", keysPath);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to create keys directory at {Path}, using ephemeral keys", keysPath);
    }
}

#endregion