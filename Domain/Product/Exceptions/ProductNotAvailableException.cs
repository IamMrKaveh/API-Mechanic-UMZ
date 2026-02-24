namespace Domain.Product.Exceptions;

public sealed class ProductNotAvailableException : DomainException
{
    public int ProductId { get; }
    public int? VariantId { get; }
    public string Reason { get; }

    public ProductNotAvailableException(int productId, string reason, int? variantId = null)
        : base($"محصول {productId} در دسترس نیست. دلیل: {reason}")
    {
        ProductId = productId;
        VariantId = variantId;
        Reason = reason;
    }

    public static ProductNotAvailableException Deleted(int productId) =>
        new(productId, "محصول حذف شده است.");

    public static ProductNotAvailableException Inactive(int productId) =>
        new(productId, "محصول غیرفعال است.");

    public static ProductNotAvailableException VariantInactive(int productId, int variantId) =>
        new(productId, "واریانت غیرفعال است.", variantId);

    public static ProductNotAvailableException OutOfStock(int productId, int variantId) =>
        new(productId, "موجودی کاف�� نیست.", variantId);
}