namespace Domain.Product.Exceptions;

public sealed class ProductNotFoundException(int productId) : DomainException($"محصول با شناسه {productId} یافت نشد.")
{
    public int ProductId { get; } = productId;
}