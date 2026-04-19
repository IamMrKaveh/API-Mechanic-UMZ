using AngleSharp;
using Application.Analytics.Contracts;
using Application.Attribute.Contracts;
using Application.Auth.Contracts;
using Application.Brand.Contracts;
using Application.Cart.Contracts;
using Application.Category.Contracts;
using Application.Discount.Contracts;
using Application.Location.Contracts;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Payment.Contracts;
using Application.Product.Contracts;
using Application.Review.Contracts;
using Application.Shipping.Contracts;
using Application.Support.Contracts;
using Application.User.Contracts;
using Application.Variant.Contracts;
using Application.Wishlist.Contracts;
using Domain.Attribute.Interfaces;
using Domain.Audit.Interfaces;
using Domain.Brand.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Category.Interfaces;
using Domain.Discount.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Media.Interfaces;
using Domain.Notification.Interfaces;
using Domain.Order.Interfaces;
using Domain.Payment.Interfaces;
using Domain.Product.Interfaces;
using Domain.Review.Interfaces;
using Domain.Security.Interfaces;
using Domain.Shipping.Interfaces;
using Domain.Support.Interfaces;
using Domain.User.Interfaces;
using Domain.Variant.Interfaces;
using Domain.Wallet.Interfaces;
using Domain.Wishlist.Interfaces;
using Infrastructure.Analytics.QueryServices;
using Infrastructure.Attribute.QueryServices;
using Infrastructure.Attribute.Repositories;
using Infrastructure.Audit.QueryServices;
using Infrastructure.Audit.Repositories;
using Infrastructure.Audit.Services;
using Infrastructure.Auth.Options;
using Infrastructure.Auth.Repositories;
using Infrastructure.Auth.Services;
using Infrastructure.BackgroundJobs;
using Infrastructure.Brand.QueryServices;
using Infrastructure.Brand.Repositories;
using Infrastructure.Cache.Redis.Services;
using Infrastructure.Cache.Services;
using Infrastructure.Cart.QueryServices;
using Infrastructure.Cart.Repositories;
using Infrastructure.Category.QueryServices;
using Infrastructure.Category.Repositories;
using Infrastructure.Common.Services;
using Infrastructure.Communication.Options;
using Infrastructure.Communication.Services;
using Infrastructure.Discount.QueryServices;
using Infrastructure.Discount.Repositories;
using Infrastructure.Discount.Services;
using Infrastructure.Inventory.QueryServices;
using Infrastructure.Inventory.Repositories;
using Infrastructure.Inventory.Services;
using Infrastructure.Location.Services;
using Infrastructure.Media.QueryServices;
using Infrastructure.Media.Repositories;
using Infrastructure.Notification.QueryServices;
using Infrastructure.Notification.Repositories;
using Infrastructure.Notification.Services;
using Infrastructure.Order.QueryServices;
using Infrastructure.Order.Repositories;
using Infrastructure.Order.Services;
using Infrastructure.Payment.Factory;
using Infrastructure.Payment.QueryServices;
using Infrastructure.Payment.Repositories;
using Infrastructure.Payment.Services;
using Infrastructure.Payment.ZarinPal;
using Infrastructure.Payment.ZarinPal.Options;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Outbox;
using Infrastructure.Product.QueryServices;
using Infrastructure.Product.Repositories;
using Infrastructure.Review.QueryServices;
using Infrastructure.Review.Repositories;
using Infrastructure.Search.Options;
using Infrastructure.Search.Services;
using Infrastructure.Security.Options;
using Infrastructure.Shipping.QueryServices;
using Infrastructure.Shipping.Repositories;
using Infrastructure.Storage.Options;
using Infrastructure.Storage.Services;
using Infrastructure.Support.QueryServices;
using Infrastructure.Support.Repositories;
using Infrastructure.User.QueryServices;
using Infrastructure.User.Repositories;
using Infrastructure.Variant.QueryServices;
using Infrastructure.Variant.Repositories;
using Infrastructure.Wallet.QueryServices;
using Infrastructure.Wallet.Repositories;
using Infrastructure.Wallet.Services;
using Infrastructure.Wishlist.QueryServices;
using Infrastructure.Wishlist.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Polly;
using DateTimeProvider = Infrastructure.Common.Services.DateTimeProvider;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddRedisCache(configuration);
        services.AddRepositories();
        services.AddQueryServices();
        services.AddDomainServices();
        services.AddAuthServices(configuration);
        services.AddPaymentServices(configuration);
        services.AddStorageServices(configuration);
        services.AddCommunicationServices(configuration);
        services.AddSearchServices(configuration);
        services.AddBackgroundServices(configuration);
        services.AddHealthChecks(configuration);
        services.AddDataProtectionWithRedis(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddHttpContextAccessor();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<DomainEventInterceptor>();

        services.AddDbContext<DBContext>((sp, options) =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(DBContext).Assembly.FullName);
                    npgsql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                })
                .AddInterceptors(
                    sp.GetRequiredService<AuditableEntityInterceptor>(),
                    sp.GetRequiredService<DomainEventInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IOutboxProcessor, OutboxProcessor>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUrlResolverService, UrlResolverService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }

    private static void AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Cache:RedisConnectionString"]
            ?? "localhost:6379";

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = configuration["Cache:KeyPrefix"] ?? "shop";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddSingleton<IDistributedLock, DistributedLockService>();
        services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOtpRepository, OtpRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IVariantRepository, VariantRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderStatusRepository, OrderStatusRepository>();
        services.AddScoped<IOrderProcessStateRepository, OrderProcessStateRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentRepository>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IShippingRepository, ShippingRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
    }

    private static void AddQueryServices(this IServiceCollection services)
    {
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IVariantQueryService, VariantQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<IBrandQueryService, BrandQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<IPaymentQueryService, PaymentQueryService>();
        services.AddScoped<IWalletQueryService, WalletQueryService>();
        services.AddScoped<IDiscountQueryService, DiscountQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<IWishlistQueryService, WishlistQueryService>();
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IShippingQueryService, ShippingQueryService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<IAttributeQueryService, AttributeQueryService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<IAnalyticsQueryService, AnalyticsQueryService>();
    }

    private static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IDiscountService, DiscountService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
        services.AddScoped<ICheckoutAddressResolverService, CheckoutAddressResolverService>();
        services.AddScoped<ICheckoutCartItemBuilderService, CheckoutCartItemBuilderService>();
        services.AddScoped<ICheckoutDiscountApplicatorService, CheckoutDiscountApplicatorService>();
        services.AddScoped<ICheckoutOrderCreationService, CheckoutOrderCreationService>();
        services.AddScoped<ICheckoutPaymentProcessorService, CheckoutPaymentProcessorService>();
        services.AddScoped<ICheckoutPriceValidatorService, CheckoutPriceValidatorService>();
        services.AddScoped<ICheckoutShippingValidatorService, CheckoutShippingValidatorService>();
        services.AddScoped<ICheckoutStockValidatorService, CheckoutStockValidatorService>();
        services.AddScoped<ILocationService, LocationService>();
    }

    private static void AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .BindConfiguration(OtpOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
    }

    private static void AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.AddScoped<IStorageService, S3FileStorageService>();
    }

    private static void AddCommunicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SmsOptions>()
            .BindConfiguration(SmsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.Configure<KavenegarOptions>(configuration.GetSection(KavenegarOptions.SectionName));

        services.AddHttpClient<ISmsService, SmsService>()
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
            .AddTransientHttpErrorPolicy(policy =>
                policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));

        services.AddScoped<IEmailService, EmailService>();
    }

    private static void AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ZarinPalOptions>()
            .BindConfiguration(ZarinPalOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<PaymentGatewayOptions>(configuration.GetSection("PaymentGateway"));

        services.AddScoped<IPaymentGatewayFactory, PaymentGatewayFactory>();
        services.AddScoped<IPaymentGateway, ZarinPalPaymentGateway>();

        services.AddHttpClient<ZarinPalPaymentGateway>()
            .AddTransientHttpErrorPolicy(policy =>
                policy.WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
            .AddTransientHttpErrorPolicy(policy =>
                policy.CircuitBreakerAsync(5, TimeSpan.FromSeconds(60)));
    }

    private static void AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        var elasticsearchSection = configuration.GetSection(ElasticsearchOptions.SectionName);
        services.Configure<ElasticsearchOptions>(elasticsearchSection);

        var options = elasticsearchSection.Get<ElasticsearchOptions>();

        if (options is not null && options.IsEnabled)
        {
            services.AddSingleton<ElasticsearchClient>(_ =>
            {
                var settings = new ElasticsearchClientSettings(new Uri(options.Url))
                    .DefaultIndex(options.DefaultIndex);
                return new ElasticsearchClient(settings);
            });

            services.AddScoped<ISearchService, ResilientElasticSearchService>();
        }
        else
        {
            services.AddScoped<ISearchService, NoOpSearchService>();
        }
    }

    private static void AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHostedService<OutboxProcessorJob>();

        var elasticsearchSection = configuration.GetSection(ElasticsearchOptions.SectionName);
        var options = elasticsearchSection.Get<ElasticsearchOptions>();

        if (options is not null && options.IsEnabled && options.EnableBackgroundSync)
        {
            services.AddHostedService<ElasticsearchSyncJob>();
        }
    }

    private static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddHealthChecks()
            .AddCheck("postgresql", new NpgsqlHealthCheck(connectionString),
                tags: ["db", "sql", "postgresql"])
            .AddRedis(
                configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "redis",
                tags: ["cache", "redis"]);
    }

    private sealed class NpgsqlHealthCheck(string connectionString) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }

    private static void AddDataProtectionWithRedis(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Cache:RedisConnectionString"]
            ?? "localhost:6379";

        services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(
                ConnectionMultiplexer.Connect(redisConnectionString),
                "DataProtection-Keys");
    }

    private static void AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>();

        services.AddAuthentication(options =>
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
                ValidIssuer = jwtOptions?.Issuer,
                ValidAudience = jwtOptions?.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions?.Secret ?? string.Empty))
            };
        });
    }
}