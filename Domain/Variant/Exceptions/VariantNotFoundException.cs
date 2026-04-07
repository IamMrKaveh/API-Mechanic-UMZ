using Domain.Common.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Exceptions;

public sealed class VariantNotFoundException : DomainException
{
    public VariantId? VariantId { get; }
    public ProductId? ProductId { get; }
    public string? Sku { get; }

    public override string ErrorCode => "VARIANT_NOT_FOUND";

    public VariantNotFoundException(VariantId variantId)
        : base($"واریانت با شناسه {variantId} یافت نشد.")
    {
        VariantId = variantId;
    }

    public VariantNotFoundException(VariantId variantId, ProductId productId)
        : base($"واریانت با شناسه {variantId} برای محصول {productId} یافت نشد.")
    {
        VariantId = variantId;
        ProductId = productId;
    }

    public VariantNotFoundException(string sku)
        : base($"واریانت با کد SKU '{sku}' یافت نشد.")
    {
        Sku = sku;
    }
}