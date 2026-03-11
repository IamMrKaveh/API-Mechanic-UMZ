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