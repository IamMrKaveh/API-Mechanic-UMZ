using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Rules;

public sealed class VariantSkuMustBeUniqueRule(
    Sku sku,
    IEnumerable<ProductVariant> existingVariants,
    ProductVariantId? excludeVariantId = null) : IBusinessRule
{
    private readonly Sku _sku = sku;
    private readonly IEnumerable<ProductVariant> _existingVariants = existingVariants;
    private readonly ProductVariantId? _excludeVariantId = excludeVariantId;

    public bool IsBroken()
    {
        if (_sku is null || string.IsNullOrEmpty(_sku.Value))
            return false;

        return _existingVariants.Any(v =>
            !v.IsDeleted &&
            (_excludeVariantId == null || v.Id != _excludeVariantId) &&
            v.Sku.Matches(_sku.Value));
    }

    public string Message => $"کد SKU '{_sku?.Value}' قبلاً در این محصول استفاده شده است.";
}