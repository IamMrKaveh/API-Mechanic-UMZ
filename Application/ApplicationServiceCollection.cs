using Application.Common.Behaviors;
using Application.Common.Mapping;
using Application.Order.Features.Commands.CheckoutFromCart.Services;
using Domain.Cart.Services;
using Domain.Inventory.Services;
using Domain.Media.Services;
using Domain.Payment.Services;
using Domain.Support.Services;
using Mapster;
using MapsterMapper;

namespace Application;

public static class ApplicationServiceCollection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        RegisterMediatR(services);
        RegisterDomainServices(services);
        RegisterApplicationServices(services);
        RegisterValidation(services);
        RegisterMapster(services);
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

    private static void RegisterValidation(IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private static void RegisterDomainServices(IServiceCollection services)
    {
        services.AddScoped<InventoryDomainService>();
        services.AddScoped<CartDomainService>();
        services.AddScoped<PaymentDomainService>();
        services.AddScoped<MediaDomainService>();
        services.AddScoped<TicketDomainService>();
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<InventoryReservationService>();
        services.AddScoped<PaymentSettlementService>();
        RegisterCheckoutServices(services);
    }

    private static void RegisterCheckoutServices(IServiceCollection services)
    {
        services.AddScoped<ICheckoutAddressResolverService, CheckoutAddressResolverService>();
        services.AddScoped<ICheckoutShippingValidatorService, CheckoutShippingValidatorService>();
        services.AddScoped<ICheckoutCartItemBuilderService, CheckoutCartItemBuilderService>();
        services.AddScoped<ICheckoutPriceValidatorService, CheckoutPriceValidatorService>();
        services.AddScoped<ICheckoutStockValidatorService, CheckoutStockValidatorService>();
        services.AddScoped<ICheckoutOrderCreationService, CheckoutOrderCreationService>();
        services.AddScoped<ICheckoutDiscountApplicatorService, CheckoutDiscountApplicatorService>();
        services.AddScoped<ICheckoutPaymentProcessorService, CheckoutPaymentProcessorService>();
        services.AddScoped<ICheckoutOrchestrationService, CheckoutOrchestrationService>();
    }

    private static void RegisterMapster(IServiceCollection services)
    {
        MapsterConfig.Configure();

        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
    }
}