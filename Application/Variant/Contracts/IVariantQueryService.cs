using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Product.Features.Shared;
using Application.Shipping.Features.Shared;

namespace Application.Variant.Contracts;

public interface IVariantQueryService
{
    Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        int productId,
        bool activeOnly,
        CancellationToken ct = default);

    Task<ProductVariantShippingInfoDto?> GetVariantShippingInfoAsync(
        int variantId,
        CancellationToken ct = default);

    Task<VariantAvailabilityDto?> GetVariantAvailabilityAsync(
        int variantId,
        CancellationToken ct = default);
}