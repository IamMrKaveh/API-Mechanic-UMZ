using Domain.Cart.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Exceptions;

namespace Domain.Cart.Exceptions;

public sealed class CartItemNotFoundException(VariantId variantId)
    : NotFoundException<VariantId>(
        "CART_ITEM_NOT_FOUND",
        variantId,
        $"آیتم سبد خرید برای واریانت {variantId} یافت نشد.")
{
    public VariantId VariantId => Value;
}

public sealed class CartAlreadyCheckedOutException(CartId cartId)
    : ConflictException<CartId>(
        "CART_ALREADY_CHECKED_OUT",
        cartId,
        $"سبد خرید {cartId} قبلاً تسویه شده است.")
{
    public CartId CartId => Value;
}

public sealed class InvalidCartQuantityException(int quantity)
    : SingleValueDomainException<int>(
        "INVALID_CART_QUANTITY",
        quantity,
        $"تعداد آیتم سبد خرید '{quantity}' نامعتبر است. تعداد باید بزرگتر از صفر باشد.")
{
    public int Quantity => Value;
}
