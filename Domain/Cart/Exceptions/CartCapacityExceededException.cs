namespace Domain.Cart.Exceptions;

public class CartCapacityExceededException : DomainException
{
    public int CartId { get; }
    public int MaxItems { get; }
    public int CurrentItems { get; }

    public CartCapacityExceededException(int cartId, int maxItems, int currentItems)
        : base($"سبد خرید به حداکثر ظرفیت ({maxItems} آیتم) رسیده است. تعداد فعلی: {currentItems}")
    {
        CartId = cartId;
        MaxItems = maxItems;
        CurrentItems = currentItems;
    }
}