using Domain.Inventory.Services;
using Domain.Media.Services;
using Domain.Payment.Services;
using Domain.Review.Services;
using Domain.Shipping.Services;
using Domain.Support.Services;

namespace Application.Common.DependencyInjection;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.RegisterMediatR();
        services.RegisterDomainServices();
        services.RegisterApplicationServices();
        services.RegisterValidators();
        services.AddApplicationMappings();
        return services;
    }

    private static IServiceCollection RegisterMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ApplicationServiceCollection).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionLoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(QueryLoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        });

        return services;
    }

    private static IServiceCollection RegisterDomainServices(this IServiceCollection services)
    {
        services.AddScoped<ShippingDomainService>();
        services.AddScoped<ReviewDomainService>();
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<PaymentDomainService>();
        services.AddScoped<MediaDomainService>();
        services.AddScoped<TicketDomainService>();

        return services;
    }

    private static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<InventoryReservationService>();
        services.AddScoped<PaymentSettlementService>();

        return services;
    }

    private static IServiceCollection RegisterValidators(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollection).Assembly);

        return services;
    }
}