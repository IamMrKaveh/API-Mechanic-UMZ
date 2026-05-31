using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Cache.Contracts;

public interface ICacheInvalidationService
{
    Task InvalidateProductCacheAsync(
        ProductId productId,
        CancellationToken ct = default);

    Task InvalidateUserCacheAsync(
        UserId userId,
        CancellationToken ct = default);

    Task InvalidateInventoryCacheAsync(
        VariantId variantId,
        CancellationToken ct = default);
}