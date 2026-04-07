using Domain.Common.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Product.Exceptions;

public sealed class DuplicateSkuException : DomainException
{
    public Sku Sku { get; }

    public override string ErrorCode => "DUPLICATE_SKU";

    public DuplicateSkuException(Sku sku, string entityType = "محصول")
        : base($"کد SKU '{sku}' قبلاً برای {entityType} دیگری ثبت شده است.")
    {
        Sku = sku;
    }
}