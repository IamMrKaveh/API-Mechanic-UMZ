#region Serilog Configuration 
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
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File(path: "logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30, fileSizeLimitBytes: 10_000_000, shared: true, flushToDiskInterval: TimeSpan.FromSeconds(1), outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();
#endregion

try
{
    Log.Information("Starting API");
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    #region Database Configuration
    var connectionString = builder.Configuration.GetConnectionString("PoolerConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Log.Fatal("Database connection string 'PoolerConnection' is missing!");
        throw new InvalidOperationException("Connection string 'PoolerConnection' not found.");
    }

    Log.Information("[RUNTIME MODE] Database connection validated: {Connection}",
        MaskConnectionString(connectionString));

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
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    IConnectionMultiplexer? redis = null;
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
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

            redis = await ConnectionMultiplexer.ConnectAsync(redisOptions);
            builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
            builder.Services.AddSingleton(sp => redis.GetDatabase());
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();

            Log.Information("Redis connected successfully.");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Redis connection failed. Falling back to in-memory and filesystem DataProtection.");
            redis = null;
            RegisterInMemoryServices(builder.Services);
        }
    }
    else
    {
        Log.Warning("Redis connection string not found. Using in-memory services.");
        RegisterInMemoryServices(builder.Services);
    }
    #endregion

    #region DataProtection Configuration
    var dataProtection = builder.Services.AddDataProtection()
        .SetApplicationName("Ledka");

    if (redis != null)
    {
        dataProtection.PersistKeysToSingletonRedisRepository(redis, "Ledka-DataProtection-Keys");
        Log.Information("DataProtection configured with Redis");
    }
    else
    {
        var keysPath = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "DataProtection-Keys"));
        if (!keysPath.Exists)
        {
            keysPath.Create();
        }
        dataProtection.PersistKeysToFileSystem(keysPath);
        Log.Information("DataProtection configured with FileSystem at {Path}", keysPath.FullName);
    }
    #endregion

    #region AutoMapper Configuration
    builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
    #endregion

    #region Application Services Registration
    builder.Services.AddApplicationServices(builder.Configuration);
    #endregion

    #region Infrastructure Services Registration
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddHostedService<Infrastructure.BackgroundJobs.PaymentCleanupService>();
    builder.Services.AddHostedService<Infrastructure.BackgroundJobs.PaymentVerificationJob>();
    builder.Services.AddHostedService<Infrastructure.BackgroundJobs.OrphanedFileCleanupService>();

    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
    builder.Services.AddSingleton<IStorageService, LiaraStorageService>();
    builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IMediaService, MediaService>();
    builder.Services.AddScoped<IPaymentService, Application.Services.PaymentService>();
    #endregion

    #region Current User Service
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    #endregion

    #region Configuration Options
    builder.Services.Configure<LiaraStorageSettings>(builder.Configuration.GetSection("LiaraStorage"));
    builder.Services.Configure<ZarinpalSettingsDto>(builder.Configuration.GetSection("Zarinpal"));
    builder.Services.Configure<SecurityHeadersOptions>(builder.Configuration.GetSection("SecurityHeaders"));
    builder.Services.Configure<FrontendUrlsDto>(builder.Configuration.GetSection("FrontendUrls"));
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
    Log.Information("Application services registered");
    #endregion

    #region Security Configuration
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
    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "XSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = false;
        options.Cookie.Path = "/";
    });
    builder.Services.AddSingleton<IHtmlSanitizer>(_ => new HtmlSanitizer(new HtmlSanitizerOptions
    {
        AllowedTags = { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
        AllowedAttributes = { "class" },
        AllowedSchemes = { "http", "https" }
    }));
    builder.Services.AddHttpContextAccessor();
    #endregion

    #region JWT Authentication Configuration
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
            ClockSkew = TimeSpan.Zero,
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
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var authHeader = context.Request.Headers["Authorization"].ToString();
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = authHeader.Substring("Bearer ".Length).Trim();
                        return Task.CompletedTask;
                    }
                }

                if (context.Request.Cookies.TryGetValue("jwt_token", out var token))
                {
                    context.Token = token;
                }
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

    #region Authorization Configuration
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    });
    #endregion

    #region HTTP Clients Configuration
    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
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

    var zarinPalRetryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests || msg.StatusCode == HttpStatusCode.RequestTimeout || msg.StatusCode == HttpStatusCode.GatewayTimeout)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("ZarinPal API retry {RetryCount} after {Timespan}s due to: {Result}",
                    retryCount, timespan.TotalSeconds, outcome.Result?.StatusCode);
            });

    builder.Services.AddHttpClient<Infrastructure.Payment.ZarinPal.ZarinPalPaymentGateway>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "Ledka/1.0");
    })
    .AddPolicyHandler(zarinPalRetryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

    builder.Services.AddScoped<IPaymentGateway, Infrastructure.Payment.ZarinPal.ZarinPalPaymentGateway>();

    builder.Services.AddHttpClient<ILocationService, LocationService>()
        .AddPolicyHandler(retryPolicy)
        .AddPolicyHandler(circuitBreakerPolicy);
    Log.Information("HTTP clients configured");
    #endregion

    #region CORS Configuration
    var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SecurePolicy", policy =>
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Content-Disposition", "X-Pagination", "Token-Expired", "X-Correlation-ID", "X-Guest-Token")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });
    Log.Information("CORS configured with origins: {Origins}", string.Join(", ", allowedOrigins));
    #endregion

    #region Output Cache Configuration
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
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ledka", Version = "v1" });
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
    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString,
            name: "postgresql",
            timeout: TimeSpan.FromSeconds(5),
            tags: new[] { "db", "ready" });
    Log.Information("Health checks configured");
    #endregion

    #region Kestrel Configuration
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

    app.UseCors("SecurePolicy");

    app.Use(async (context, next) =>
    {
        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return;
        }
        await next();
    });

    app.UseSecurityMiddleware();

    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
            c.RoutePrefix = "swagger";
        });
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.Response.Headers.Remove("Content-Security-Policy");
                context.Response.Headers.Remove("Content-Security-Policy-Report-Only");
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
    app.UseDefaultFiles();

    var staticFileOptions = new StaticFileOptions
    {
        ServeUnknownFileTypes = false,
        OnPrepareResponse = ctx =>
        {
            var path = ctx.File.Name.ToLowerInvariant();

            if (path == "index.html")
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
                ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                ctx.Context.Response.Headers.Append("Expires", "0");
            }
            else if (path.EndsWith(".js") || path.EndsWith(".css") || path.EndsWith(".woff2") || path.EndsWith(".png") || path.EndsWith(".jpg"))
            {
                ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
            }
        }
    };

    var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
    provider.Mappings[".js"] = "application/javascript";
    provider.Mappings[".mjs"] = "application/javascript";
    provider.Mappings[".css"] = "text/css";
    provider.Mappings[".json"] = "application/json";
    provider.Mappings[".woff"] = "font/woff";
    provider.Mappings[".woff2"] = "font/woff2";
    provider.Mappings[".ttf"] = "font/ttf";
    provider.Mappings[".eot"] = "application/vnd.ms-fontobject";
    provider.Mappings[".svg"] = "image/svg+xml";
    provider.Mappings[".png"] = "image/png";
    provider.Mappings[".jpg"] = "image/jpeg";
    provider.Mappings[".jpeg"] = "image/jpeg";
    provider.Mappings[".gif"] = "image/gif";
    provider.Mappings[".webp"] = "image/webp";
    provider.Mappings[".ico"] = "image/x-icon";
    provider.Mappings[".webmanifest"] = "application/manifest+json";

    staticFileOptions.ContentTypeProvider = provider;

    app.UseStaticFiles(staticFileOptions);

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();
    app.UseOutputCache();

    Log.Information("Middleware pipeline configured");
    #endregion

    #region Endpoint Mapping
    app.MapControllers();
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
        }
    });

    app.MapFallbackToFile("index.html");

    Log.Information("Endpoints mapped");
    #endregion

    #region Run Application
    Log.Information("Application starting on {Urls}", string.Join(", ", app.Urls));
    await app.RunAsync();
    #endregion
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static void RegisterInMemoryServices(IServiceCollection services)
{
    services.AddMemoryCache();
    services.AddSingleton<ICacheService, InMemoryCacheService>();
}

static string MaskConnectionString(string connectionString)
{
    try
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.Password))
        {
            builder.Password = "****";
        }
        if (!string.IsNullOrEmpty(builder.Username))
        {
            builder.Username = "****";
        }
        return builder.ToString();
    }
    catch
    {
        return "******";
    }
}