using Application.Cache.Contracts;
using Application.Common.Events;
using Domain.Product.Events;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Cache.EventHandlers;

namespace Tests.Infrastructure.Cache.EventHandlers;

public class ProductCacheInvalidationHandlerTests
{
    private readonly ICacheInvalidationService _invalidation = Substitute.For<ICacheInvalidationService>(); private readonly ProductCacheInvalidationHandler _sut;

    public ProductCacheInvalidationHandlerTests()
    {
        _sut = new ProductCacheInvalidationHandler(_invalidation);
    }

    [Fact]
    public async Task Handle_ProductUpdatedEvent_InvalidatesProductCacheForEventProductId()
    {
        var productId = ProductId.NewId();
        var evt = new ProductUpdatedEvent(
            productId,
            ProductName.Create("Sample Product"),
            ProductSlug.GenerateFrom("Sample Product"),
            "desc");
        var notification = new DomainEventNotification<ProductUpdatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateProductCacheAsync(productId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PriceChangedEvent_InvalidatesProductCacheForEventProductId()
    {
        var productId = ProductId.NewId();
        var variantId = VariantId.NewId();
        var evt = new PriceChangedEvent(variantId, productId, 100m, 120m, 100m, 120m);
        var notification = new DomainEventNotification<PriceChangedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateProductCacheAsync(productId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductActivatedEvent_InvalidatesProductCacheForEventProductId()
    {
        var productId = ProductId.NewId();
        var evt = new ProductActivatedEvent(productId);
        var notification = new DomainEventNotification<ProductActivatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateProductCacheAsync(productId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProductDeactivatedEvent_InvalidatesProductCacheForEventProductId()
    {
        var productId = ProductId.NewId();
        var evt = new ProductDeactivatedEvent(productId);
        var notification = new DomainEventNotification<ProductDeactivatedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _invalidation.Received(1).InvalidateProductCacheAsync(productId, Arg.Any<CancellationToken>());
    }
}
