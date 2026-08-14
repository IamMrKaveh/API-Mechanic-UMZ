using Application.Cache.Contracts;
using Application.Cache.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Cache.Services;

namespace Tests.Infrastructure.Cache.Services;

public class CacheInvalidationServiceTests
{
    private readonly ICacheService _cache = Substitute.For<ICacheService>(); private readonly CacheInvalidationService _sut;

    public CacheInvalidationServiceTests()
    {
        _sut = new CacheInvalidationService(_cache);
    }

    [Fact]
    public async Task InvalidateProductCacheAsync_RemovesSpecificProductKeyAndProductsPrefix()
    {
        var productId = ProductId.NewId();

        await _sut.InvalidateProductCacheAsync(productId, CancellationToken.None);

        await _cache.Received(1).RemoveAsync(CacheKeys.Product(productId.Value), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveByPrefixAsync("products:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InvalidateUserCacheAsync_RemovesUserProfileKey()
    {
        var userId = UserId.NewId();

        await _sut.InvalidateUserCacheAsync(userId, CancellationToken.None);

        await _cache.Received(1).RemoveAsync(CacheKeys.UserProfile(userId.Value), Arg.Any<CancellationToken>());
        await _cache.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task InvalidateInventoryCacheAsync_RemovesInventoryKeyForVariant()
    {
        var variantId = VariantId.NewId();

        await _sut.InvalidateInventoryCacheAsync(variantId, CancellationToken.None);

        await _cache.Received(1).RemoveAsync(CacheKeys.Inventory(variantId.Value), Arg.Any<CancellationToken>());
        await _cache.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task InvalidateProductCacheAsync_PropagatesCancellationTokenToCache()
    {
        var productId = ProductId.NewId();
        using var cts = new CancellationTokenSource();

        await _sut.InvalidateProductCacheAsync(productId, cts.Token);

        await _cache.Received(1).RemoveAsync(CacheKeys.Product(productId.Value), cts.Token);
        await _cache.Received(1).RemoveByPrefixAsync("products:", cts.Token);
    }
}
