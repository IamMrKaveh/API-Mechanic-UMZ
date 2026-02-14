namespace Domain.Product.Exceptions;

public sealed class ProductNotFoundException : DomainException
{
    public int ProductId { get; }

    public ProductNotFoundException(int productId)
        : base($"محصول با شناسه {productId} یافت نشد.")
    {
        ProductId = productId;
    }
}