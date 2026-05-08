using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Converters;

internal sealed class OrderItemIdConverter : StronglyTypedIdConverter<OrderItemId>
{
    public OrderItemIdConverter() : base(OrderItemId.From)
    {
    }
}