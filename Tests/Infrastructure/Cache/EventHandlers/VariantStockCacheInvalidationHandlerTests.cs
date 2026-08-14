using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Cache.Features.Shared;
using Application.Variant.Features.Shared;
using Infrastructure.Cache.EventHandlers;

namespace Tests.Infrastructure.Cache.EventHandlers;

public class VariantStockCacheInvalidationHandlerTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly IAuditService _audit = Substitute.For<IAuditService>(); private readonly VariantStockCacheInvalidationHandler _sut;

    public VariantStockCacheInvalidationHandlerTests()
    {
        _sut = new VariantStockCacheInvalidationHandler(_cache, _audit);
    }

    private static VariantStockChangedApplicationNotification BuildNotification(
        Guid variantId,
        Guid productId,
        int newAvailable,
        bool isInStock)
        => new()
        {
            VariantId = variantId,
            ProductId = productId,
            QuantityChanged = 1,
            NewOnHand = newAvailable,
            NewReserved = 0,
            NewAvailable = newAvailable,
            IsInStock = isInStock
        };

    [Fact]
    public async Task Handle_RemovesBothVariantAndProductAvailabilityCacheKeys()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var notification = BuildNotification(variantId, productId, newAvailable: 10, isInStock: true);

        await _sut.Handle(notification, CancellationToken.None);

        await _cache.Received(1).RemoveAsync($"inventory:availability:{variantId}", Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"inventory:product-availability:{productId}", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNewAvailableIsPositive_SetsCacheWithTwoMinuteExpiry()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var notification = BuildNotification(variantId, productId, newAvailable: 10, isInStock: true);

        VariantAvailabilityCache? captured = null;
        TimeSpan? capturedExpiry = null;
        string? capturedKey = null;

        await _cache.SetAsync(
            Arg.Do<string>(k => capturedKey = k),
            Arg.Do<VariantAvailabilityCache>(v => captured = v),
            Arg.Do<TimeSpan?>(t => capturedExpiry = t),
            Arg.Any<CancellationToken>());

        await _sut.Handle(notification, CancellationToken.None);

        capturedKey.ShouldBe($"inventory:availability:{variantId}");
        capturedExpiry.ShouldBe(TimeSpan.FromMinutes(2));
        captured.ShouldNotBeNull();
        captured!.VariantId.ShouldBe(variantId);
        captured.AvailableQuantity.ShouldBe(10);
        captured.IsUnlimited.ShouldBeFalse();
        captured.IsInStock.ShouldBeTrue();
        captured.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenNewAvailableIsNegative_DoesNotSetCache()
    {
        var notification = BuildNotification(Guid.NewGuid(), Guid.NewGuid(), newAvailable: -1, isInStock: false);

        await _sut.Handle(notification, CancellationToken.None);

        await _cache.DidNotReceiveWithAnyArgs().SetAsync(
            default!,
            default(VariantAvailabilityCache)!,
            default,
            default);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    [InlineData(100, false)]
    public async Task Handle_ComputesIsLowStockFlagBasedOnNewAvailable(int newAvailable, bool expectedIsLowStock)
    {
        var notification = BuildNotification(Guid.NewGuid(), Guid.NewGuid(), newAvailable, isInStock: newAvailable > 0);

        VariantAvailabilityCache? captured = null;
        await _cache.SetAsync(
            Arg.Any<string>(),
            Arg.Do<VariantAvailabilityCache>(v => captured = v),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());

        await _sut.Handle(notification, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.IsLowStock.ShouldBe(expectedIsLowStock);
    }

    [Fact]
    public async Task Handle_WhenSuccessful_WritesDebugAuditEntry()
    {
        var notification = BuildNotification(Guid.NewGuid(), Guid.NewGuid(), newAvailable: 3, isInStock: true);

        await _sut.Handle(notification, CancellationToken.None);

        await _audit.Received(1).LogDebugAsync(
            Arg.Is<string>(s => s!.Contains(notification.VariantId.ToString()) && s!.Contains(notification.ProductId.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheThrows_LogsErrorAndSwallowsException()
    {
        var notification = BuildNotification(Guid.NewGuid(), Guid.NewGuid(), newAvailable: 2, isInStock: true);

        _cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("boom"));

        var act = () => _sut.Handle(notification, CancellationToken.None);

        await act.ShouldNotThrowAsync();
        await _audit.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s!.Contains(notification.VariantId.ToString())),
            Arg.Any<CancellationToken>());
    }
}
