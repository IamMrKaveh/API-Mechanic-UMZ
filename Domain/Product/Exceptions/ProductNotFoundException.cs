using Domain.Product.ValueObjects;

namespace Domain.Product.Exceptions;

public sealed class ProductNotFoundException : DomainException
{
    public ProductId ProductId { get; }

    public override string ErrorCode => "PRODUCT_NOT_FOUND";

    public ProductNotFoundException(ProductId productId)
        : base($"محصول با شناسه {productId} یافت نشد.")
    {
        ProductId = productId;
    }
}