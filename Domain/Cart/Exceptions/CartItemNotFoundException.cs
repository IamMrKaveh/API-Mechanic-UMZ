namespace Domain.Cart.Exceptions;

public class CartItemNotFoundException : DomainException
{
    public int CartId { get; }
    public int VariantId { get; }

    public CartItemNotFoundException(int cartId, int variantId)
        : base($"آیتم با واریانت {variantId} در سبد خرید {cartId} یافت نشد.")
    {
        CartId = cartId;
        VariantId = variantId;
    }
}