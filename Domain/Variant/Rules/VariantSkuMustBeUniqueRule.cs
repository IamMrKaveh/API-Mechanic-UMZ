namespace Domain.Variant.Rules;

public sealed class VariantSkuMustBeUniqueRule : IBusinessRule
{
    private readonly string _sku;
    private readonly IEnumerable<ProductVariant> _existingVariants;
    private readonly int? _excludeVariantId;

    public VariantSkuMustBeUniqueRule(string sku, IEnumerable<ProductVariant> existingVariants, int? excludeVariantId = null)
    {
        _sku = sku?.Trim().ToUpperInvariant() ?? string.Empty;
        _existingVariants = existingVariants;
        _excludeVariantId = excludeVariantId;
    }

    public bool IsBroken()
    {
        if (string.IsNullOrEmpty(_sku)) return false;

        return _existingVariants.Any(v =>
            !v.IsDeleted &&
            (_excludeVariantId == null || v.Id != _excludeVariantId) &&
            string.Equals(v.Sku.Value, _sku, StringComparison.OrdinalIgnoreCase));
    }

    public string Message => $"کد SKU '{_sku}' قبلاً در این محصول استفاده شده است.";
}