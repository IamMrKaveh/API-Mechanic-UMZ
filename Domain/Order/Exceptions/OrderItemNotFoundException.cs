namespace Domain.Order.Exceptions;

public sealed class OrderItemNotFoundException : DomainException
{
    public int OrderId { get; }
    public int VariantId { get; }

    public OrderItemNotFoundException(int orderId, int variantId)
        : base($"آیتم با واریانت {variantId} در سفارش {orderId} یافت نشد.")
    {
        OrderId = orderId;
        VariantId = variantId;
    }
}