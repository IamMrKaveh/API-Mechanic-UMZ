using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Product.Features.Shared;
using Application.Shipping.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Contracts;

public interface IVariantQueryService
{
    Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        ProductId productId,
        bool activeOnly,
        CancellationToken ct = default);

    Task<ProductVariantShippingInfoDto?> GetVariantShippingInfoAsync(
        VariantId variantId,
        CancellationToken ct = default);

    Task<VariantAvailabilityDto?> GetVariantAvailabilityAsync(
        VariantId variantId,
        CancellationToken ct = default);
}