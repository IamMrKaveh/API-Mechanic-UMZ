namespace Domain.Variant.Exceptions;

public sealed class VariantNotFoundException : DomainException
{
    public ProductVariantId? VariantId { get; }
    public ProductId? ProductId { get; }

    public VariantNotFoundException(ProductVariantId variantId)
        : base($"واریانت با شناسه {variantId} یافت نشد.")
    {
        VariantId = variantId;
    }

    public VariantNotFoundException(ProductVariantId variantId, ProductId productId)
        : base($"واریانت با شناسه {variantId} برای محصول {productId} یافت نشد.")
    {
        VariantId = variantId;
        ProductId = productId;
    }

    public VariantNotFoundException(string sku)
        : base($"واریانت با کد SKU '{sku}' یافت نشد.")
    { }
}