using Application.Cache.Features.Shared;

namespace Tests.Application.Cache.Features.Shared;

public class VariantAvailabilityCacheTests
{
    [Fact]
    public void Constructor_StoresAllProperties()
    {
        var variantId = Guid.NewGuid();
        var cachedAt = DateTime.UtcNow;

        var cache = new VariantAvailabilityCache(variantId, 15, false, true, false, cachedAt);

        cache.VariantId.ShouldBe(variantId);
        cache.AvailableQuantity.ShouldBe(15);
        cache.IsUnlimited.ShouldBeFalse();
        cache.IsInStock.ShouldBeTrue();
        cache.IsLowStock.ShouldBeFalse();
        cache.CachedAt.ShouldBe(cachedAt);
    }

    [Fact]
    public void UnlimitedVariant_CanBeOutOfStockFlagsIndependent()
    {
        var cache = new VariantAvailabilityCache(Guid.NewGuid(), 0, true, true, false, DateTime.UtcNow);

        cache.IsUnlimited.ShouldBeTrue();
        cache.AvailableQuantity.ShouldBe(0);
        cache.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void OutOfStockVariant_StoresFlags()
    {
        var cache = new VariantAvailabilityCache(Guid.NewGuid(), 0, false, false, false, DateTime.UtcNow);

        cache.IsInStock.ShouldBeFalse();
        cache.IsLowStock.ShouldBeFalse();
        cache.AvailableQuantity.ShouldBe(0);
    }

    [Fact]
    public void LowStockVariant_StoresFlags()
    {
        var cache = new VariantAvailabilityCache(Guid.NewGuid(), 2, false, true, true, DateTime.UtcNow);

        cache.IsLowStock.ShouldBeTrue();
        cache.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void Record_ValueEquality_Works()
    {
        var variantId = Guid.NewGuid();
        var cachedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new VariantAvailabilityCache(variantId, 5, false, true, true, cachedAt);
        var b = new VariantAvailabilityCache(variantId, 5, false, true, true, cachedAt);

        a.ShouldBe(b);
    }

    [Fact]
    public void Record_WithExpression_PreservesOthers()
    {
        var cache = new VariantAvailabilityCache(Guid.NewGuid(), 5, false, true, false, DateTime.UtcNow);

        var updated = cache with { AvailableQuantity = 0, IsInStock = false };

        updated.AvailableQuantity.ShouldBe(0);
        updated.IsInStock.ShouldBeFalse();
        updated.VariantId.ShouldBe(cache.VariantId);
        updated.CachedAt.ShouldBe(cache.CachedAt);
    }
}
