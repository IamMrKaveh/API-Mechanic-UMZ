namespace Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        RegisterMediatR(services);
        RegisterAutoMapper(services);
        RegisterDomainServices(services);
        RegisterApplicationServices(services);
        return services;
    }

    private static void RegisterMediatR(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });
    }

    private static void RegisterAutoMapper(IServiceCollection services)
    {
        //services.AddAutoMapper(Assembly.GetExecutingAssembly());
    }

    private static void RegisterDomainServices(IServiceCollection services)
    {
        services.AddScoped<PriceCalculatorService>();
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<UserDomainService>();
        services.AddScoped<CategoryDomainService>();
        services.AddScoped<CartDomainService>();
        services.AddScoped<OrderDomainService>();
        services.AddScoped<PaymentDomainService>();
        services.AddScoped<MediaDomainService>();
        services.AddScoped<TicketDomainService>();
        services.AddScoped<NotificationDomainService>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<InventoryReservationService>();
        services.AddScoped<DiscountApplicationService>();
        services.AddScoped<PaymentSettlementService>();
    }
}