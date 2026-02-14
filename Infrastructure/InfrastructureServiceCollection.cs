using IConfigurationProvider = AutoMapper.IConfigurationProvider;

namespace Infrastructure;

public static class InfrastructureServiceCollection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LoggerFactory required by AutoMapper 16
        var loggerFactory = new NullLoggerFactory();

        var mapperConfig = new MapperConfiguration(
            cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.AllowNullDestinationValues = true;

                // Register mapping profiles
                cfg.AddMaps(typeof(MappingProfile).Assembly);
            },
            loggerFactory
        );

        mapperConfig.AssertConfigurationIsValid();

        services.AddSingleton<IConfigurationProvider>(mapperConfig);
        services.AddSingleton<IMapper>(sp =>
            mapperConfig.CreateMapper(sp.GetService));

        // -----------------------------
        // Unit of Work & Domain Events
        // -----------------------------
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // -----------------------------
        // Auth & Session
        // -----------------------------
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // -----------------------------
        // Repositories
        // -----------------------------
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IAttributeRepository, AttributeRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // -----------------------------
        // Query Services
        // -----------------------------
        services.AddScoped<IUserQueryService, UserQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<ICartQueryService, CartQueryService>();
        services.AddScoped<IProductQueryService, ProductQueryService>();
        services.AddScoped<IInventoryQueryService, InventoryQueryService>();
        services.AddScoped<IOrderQueryService, OrderQueryService>();
        services.AddScoped<IMediaQueryService, MediaQueryService>();
        services.AddScoped<IReviewQueryService, ReviewQueryService>();
        services.AddScoped<INotificationService, NotificationService>();

        // -----------------------------
        // Infrastructure
        // -----------------------------
        services.AddMemoryCache();

        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
        }

        services.AddHealthChecks();

        return services;
    }
}