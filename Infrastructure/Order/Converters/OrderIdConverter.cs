using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Converters;

internal sealed class OrderIdConverter : StronglyTypedIdConverter<OrderId>
{
    public OrderIdConverter() : base(OrderId.From)
    {
    }
}