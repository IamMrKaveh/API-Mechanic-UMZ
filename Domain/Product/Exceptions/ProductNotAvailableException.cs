using Domain.Common.Exceptions;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Exceptions;

public sealed class ProductNotAvailableException : DomainException
{
    public ProductId ProductId { get; }
    public VariantId? VariantId { get; }

    public override string ErrorCode => "PRODUCT_NOT_AVAILABLE";

    public ProductNotAvailableException(ProductId productId, string reason, VariantId? variantId = null)
        : base($"محصول {productId} با شناسه واریانت {variantId} در دسترس نیست. دلیل: {reason}")
    {
        ProductId = productId;
        VariantId = variantId;
    }

    public static ProductNotAvailableException Deleted(ProductId productId) =>
        new(productId, "محصول حذف شده است.");

    public static ProductNotAvailableException Inactive(ProductId productId) =>
        new(productId, "محصول غیرفعال است.");

    public static ProductNotAvailableException VariantInactive(ProductId productId, VariantId variantId) =>
        new(productId, "واریانت غیرفعال است.", variantId);

    public static ProductNotAvailableException OutOfStock(ProductId productId, VariantId variantId) =>
        new(productId, "موجودی کافی نیست.", variantId);
}