namespace Domain.Product.Exceptions;

public class DuplicateSkuException(string sku, string entityType = "محصول") : DomainException($"کد SKU '{sku}' قبلاً برای {entityType} دیگری ثبت شده است.")
{
    public string Sku { get; } = sku;
    public string EntityType { get; } = entityType;
}