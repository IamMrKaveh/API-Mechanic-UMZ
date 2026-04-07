using Domain.Cart.ValueObjects;
using Domain.Common.Exceptions;
using Domain.Variant.ValueObjects;

namespace Domain.Cart.Exceptions;

public sealed class CartNotFoundException : DomainException
{
    public CartId CartId { get; }

    public override string ErrorCode => "CART_NOT_FOUND";

    public CartNotFoundException(CartId cartId)
        : base($"سبد خرید با شناسه {cartId} یافت نشد.")
    {
        CartId = cartId;
    }
}

public sealed class CartItemNotFoundException : DomainException
{
    public VariantId VariantId { get; }

    public override string ErrorCode => "CART_ITEM_NOT_FOUND";

    public CartItemNotFoundException(VariantId variantId)
        : base($"آیتم سبد خرید برای واریانت {variantId} یافت نشد.")
    {
        VariantId = variantId;
    }
}

public sealed class CartAlreadyCheckedOutException : DomainException
{
    public CartId CartId { get; }

    public override string ErrorCode => "CART_ALREADY_CHECKED_OUT";

    public CartAlreadyCheckedOutException(CartId cartId)
        : base($"سبد خرید {cartId} قبلاً تسویه شده است.")
    {
        CartId = cartId;
    }
}

public sealed class InvalidCartQuantityException : DomainException
{
    public int Quantity { get; }

    public override string ErrorCode => "INVALID_CART_QUANTITY";

    public InvalidCartQuantityException(int quantity)
        : base($"تعداد آیتم سبد خرید '{quantity}' نامعتبر است. تعداد باید بزرگتر از صفر باشد.")
    {
        Quantity = quantity;
    }
}

public sealed class InsufficientStockForCartException : DomainException
{
    public VariantId VariantId { get; }
    public int RequestedQuantity { get; }
    public int AvailableStock { get; }

    public override string ErrorCode => "INSUFFICIENT_STOCK_FOR_CART";

    public InsufficientStockForCartException(VariantId variantId, int requestedQuantity, int availableStock)
        : base($"موجودی کافی برای واریانت {variantId} نیست. درخواستی: {requestedQuantity}، موجودی: {availableStock}.")
    {
        VariantId = variantId;
        RequestedQuantity = requestedQuantity;
        AvailableStock = availableStock;
    }
}