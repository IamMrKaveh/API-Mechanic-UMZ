namespace Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
        });

        services.AddAutoMapper(Assembly.GetExecutingAssembly());

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

        return services;
    }
}