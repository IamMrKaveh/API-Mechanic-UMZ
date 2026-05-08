using Domain.Support.ValueObjects;

namespace Infrastructure.Support.Converters;

internal sealed class TicketIdConverter : StronglyTypedIdConverter<TicketId>
{
    public TicketIdConverter() : base(TicketId.From)
    {
    }
}