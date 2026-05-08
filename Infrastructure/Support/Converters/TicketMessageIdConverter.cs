using Domain.Support.ValueObjects;

namespace Infrastructure.Support.Converters;

internal sealed class TicketMessageIdConverter : StronglyTypedIdConverter<TicketMessageId>
{
    public TicketMessageIdConverter() : base(TicketMessageId.From)
    {
    }
}