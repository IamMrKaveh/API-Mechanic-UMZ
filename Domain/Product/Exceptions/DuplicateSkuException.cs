namespace Domain.Product.Exceptions;

public class DuplicateSkuException : DomainException
{
    public string Sku { get; }
    public string EntityType { get; }

    public DuplicateSkuException(string sku, string entityType = "محصول")
        : base($"کد SKU '{sku}' قبلاً برای {entityType} دیگری ثبت شده است.")
    {
        Sku = sku;
        EntityType = entityType;
    }
}