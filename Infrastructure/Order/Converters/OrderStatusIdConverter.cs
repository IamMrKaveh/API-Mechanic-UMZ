using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Converters;

internal sealed class OrderStatusIdConverter : StronglyTypedIdConverter<OrderStatusId>
{
    public OrderStatusIdConverter() : base(OrderStatusId.From)
    {
    }
}