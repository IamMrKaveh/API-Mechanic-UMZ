using Application.Common.DependencyInjection;
using Application.Common.Services;
using Application.Payment.Adapters;
using Application.Payment.Contracts;
using Domain.Inventory.Services;
using Domain.Media.Services;
using Domain.Payment.Services;
using Domain.Review.Services;
using Domain.Shipping.Services;
using Domain.Support.Services;

namespace Tests.Application.Common.DependencyInjection;

public class ApplicationServiceCollectionTests
{
    [Fact]
    public void AddApplicationServices_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddApplicationServices();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddApplicationServices_RegistersDomainServices()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services.ShouldContain(s => s.ServiceType == typeof(ShippingDomainService) && s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(ReviewDomainService) && s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(InventoryDomainService) && s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(PaymentDomainService) && s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(MediaDomainService) && s.Lifetime == ServiceLifetime.Scoped);
        services.ShouldContain(s => s.ServiceType == typeof(TicketDomainService) && s.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplicationServices_RegistersApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services.ShouldContain(s => s.ServiceType == typeof(IAuditContextEnricher) && s.ImplementationType == typeof(AuditContextEnricher));
        services.ShouldContain(s => s.ServiceType == typeof(InventoryReservationService));
        services.ShouldContain(s => s.ServiceType == typeof(PaymentSettlementService));
        services.ShouldContain(s => s.ServiceType == typeof(IPaymentInitiator) && s.ImplementationType == typeof(PaymentInitiator));
    }

    [Fact]
    public void AddApplicationServices_RegistersMediatRAndPipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services.ShouldContain(s => s.ServiceType == typeof(IPipelineBehavior<,>));
        var behaviors = services.Where(s => s.ServiceType == typeof(IPipelineBehavior<,>)).ToList();
        behaviors.Count.ShouldBeGreaterThanOrEqualTo(9);
    }

    [Fact]
    public void AddApplicationServices_RegistersValidatorsFromAssembly()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services.ShouldContain(s => s.ServiceType.IsGenericType &&
            s.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>));
    }

    [Fact]
    public void AddApplicationServices_RegistersMappingsAsSingletonConfig()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        services.ShouldContain(s => s.ServiceType == typeof(TypeAdapterConfig) && s.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddApplicationServices_CanBuildProviderAndResolveDependencyFreeServices()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();

        using var provider = services.BuildServiceProvider();

        provider.GetService<ShippingDomainService>().ShouldNotBeNull();
        provider.GetService<InventoryDomainService>().ShouldNotBeNull();
        provider.GetRequiredService<TypeAdapterConfig>().ShouldNotBeNull();
    }
}
