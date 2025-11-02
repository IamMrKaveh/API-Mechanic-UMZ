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

try
{
    Log.Information("Starting Ledka");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var connectionString = builder.Configuration.GetConnectionString("PoolerConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Log.Fatal("Database connection string 'PoolerConnection' is missing!");
        throw new InvalidOperationException("Connection string 'PoolerConnection' not found.");
    }

    Log.Information("[RUNTIME MODE] Database connection validated: {Connection}",
        MaskConnectionString(connectionString));

    builder.Services.AddDbContextPool<MechanicContext>((serviceProvider, options) =>
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

        options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }, poolSize: 15);

    Log.Information("Database context registered successfully (EF Core 8 + Supabase)");

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");

    builder.Services.AddMemoryCache();
    var dataProtectionBuilder = builder.Services.AddDataProtection()
        .SetApplicationName("Ledka")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

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
            redisOptions.ConnectRetry = 3;

            var redisConnection = ConnectionMultiplexer.Connect(redisOptions);
            builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);
            builder.Services.AddSingleton(sp => redisConnection.GetDatabase());
            builder.Services.AddSingleton<ICacheService, RedisCacheService>();
            dataProtectionBuilder.PersistKeysToSingletonRedisRepository(redisConnection, "DataProtection-Keys");

            Log.Information("✅ Redis connected successfully and configured for Data Protection.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Redis connection failed, using in-memory cache and file system for Data Protection.");
            RegisterInMemoryServices(builder, dataProtectionBuilder);
        }
    }
    else
    {
        Log.Warning("⚠️ Redis connection string not found — using in-memory cache and file system for Data Protection.");
        RegisterInMemoryServices(builder, dataProtectionBuilder);
    }

    builder.Services.AddScoped<ICartService, CartService>();
    builder.Services.AddSingleton<IRateLimitService, RateLimitService>();
    builder.Services.AddScoped<IAuditService, AuditService>();
    builder.Services.AddSingleton<IStorageService, LiaraStorageService>();


    builder.Services.Configure<LiaraStorageSettings>(builder.Configuration.GetSection("LiaraStorage"));
    builder.Services.Configure<ZarinpalSettings>(builder.Configuration.GetSection("Zarinpal"));
    builder.Services.Configure<SecurityHeadersOptions>(builder.Configuration.GetSection("SecurityHeaders"));

    Log.Information("Application services registered");

    var maxRequestSize = builder.Configuration.GetValue<long>("Security:MaxRequestSizeInBytes", 10_485_760);
    builder.Services.Configure<KestrelServerOptions>(options =>
    {
        options.Limits.MaxRequestBodySize = maxRequestSize;
    });
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = maxRequestSize;
        options.ValueLengthLimit = (int)maxRequestSize;
    });

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "X-XSRF-TOKEN";
        options.Cookie.Name = "__Host-XSRF-TOKEN";
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
    });

    builder.Services.AddSingleton<IHtmlSanitizer>(_ => new HtmlSanitizer(new HtmlSanitizerOptions
    {
        AllowedTags = { "b", "i", "em", "strong", "p", "br", "ul", "ol", "li" },
        AllowedAttributes = { "class" },
        AllowedSchemes = { "http", "https" }
    }));

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    });

    builder.Services.AddIpWhitelist(builder.Configuration);

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

    var retryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Zarinpal retry {RetryCount} after {Timespan}s due to: {Result}",
                    retryCount, timespan.TotalSeconds, outcome.Result?.StatusCode);
            });

    var circuitBreakerPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
            onBreak: (outcome, duration) =>
                Log.Error("Zarinpal circuit breaker opened for {Duration}s", duration.TotalSeconds),
            onReset: () =>
                Log.Information("Zarinpal circuit breaker reset"));

    builder.Services.AddHttpClient<IZarinpalService, ZarinpalService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("User-Agent", "MechanicAPI/1.0");
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);

    Log.Information("HTTP clients configured");

    var allowedOrigins = builder.Configuration.GetSection("Security:AllowedOrigins").Get<string[]>()
        ?? new[] { "https://ledka-co.ir", "https://www.ledka-co.ir" };

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

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    }).AddMvc();

    Log.Information("Controllers and caching configured");

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

    var healthChecksBuilder = builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString,
            name: "postgresql",
            timeout: TimeSpan.FromSeconds(5),
            tags: new[] { "db", "ready" });

    Log.Information("Health checks configured");

    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });

    builder.Host.ConfigureHostOptions(options =>
        options.ShutdownTimeout = TimeSpan.FromSeconds(30));

    Log.Information("Kestrel configured");

    var app = builder.Build();

    Log.Information("Application built successfully");

    if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseHttpsRedirection();
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ledka v1");
            c.RoutePrefix = "swagger";
        });

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
                traceId = Activity.Current?.Id ?? context.TraceIdentifier
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
    app.UseOutputCache();

    Log.Information("Middleware pipeline configured");

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

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

    lifetime.ApplicationStarted.Register(() =>
        Log.Information("Ledka started successfully in {Environment} mode.",
            app.Environment.EnvironmentName));

    lifetime.ApplicationStopping.Register(() =>
        Log.Information("Application is shutting down gracefully..."));

    lifetime.ApplicationStopped.Register(() =>
    {
        var redis = app.Services.GetService<IConnectionMultiplexer>();
        redis?.Dispose();
        Log.Information("Application stopped.");
    });

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

static void RegisterInMemoryServices(WebApplicationBuilder builder, IDataProtectionBuilder dataProtectionBuilder)
{
    builder.Services.AddSingleton<ICacheService, MockRedisDatabase>();
    builder.Services.AddOutputCache();

    var keysPath = builder.Configuration.GetValue<string>("Security:DataProtectionPath") ?? "./keys";
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