namespace Domain.Product.Exceptions;

public sealed class ProductNotAvailableException(int productId, string reason, int? variantId = null) : DomainException($"محصول {productId} در دسترس نیست. دلیل: {reason}")
{
    public int ProductId { get; } = productId;
    public int? VariantId { get; } = variantId;
    public string Reason { get; } = reason;

    public static ProductNotAvailableException Deleted(int productId) =>
        new(productId, "محصول حذف شده است.");

    public static ProductNotAvailableException Inactive(int productId) =>
        new(productId, "محصول غیرفعال است.");

    public static ProductNotAvailableException VariantInactive(int productId, int variantId) =>
        new(productId, "واریانت غیرفعال است.", variantId);

    public static ProductNotAvailableException OutOfStock(int productId, int variantId) =>
        new(productId, "موجودی کاف�� نیست.", variantId);
}