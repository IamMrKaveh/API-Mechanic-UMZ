using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Exceptions;

public sealed class CartItemNotFoundException(VariantId variantId) : DomainException($"آیتم سبد خرید برای واریانت {variantId} یافت نشد.")
{
    public VariantId VariantId { get; } = variantId;

    public override string ErrorCode => "CART_ITEM_NOT_FOUND";
}

public sealed class CartAlreadyCheckedOutException(CartId cartId) : DomainException($"سبد خرید {cartId} قبلاً تسویه شده است.")
{
    public CartId CartId { get; } = cartId;

    public override string ErrorCode => "CART_ALREADY_CHECKED_OUT";
}

public sealed class InvalidCartQuantityException(int quantity) : DomainException($"تعداد آیتم سبد خرید '{quantity}' نامعتبر است. تعداد باید بزرگتر از صفر باشد.")
{
    public int Quantity { get; } = quantity;

    public override string ErrorCode => "INVALID_CART_QUANTITY";
}