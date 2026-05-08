using Domain.Security.ValueObjects;

namespace Infrastructure.Security.Converters;

internal sealed class SessionIdConverter : StronglyTypedIdConverter<SessionId>
{
    public SessionIdConverter() : base(SessionId.From)
    {
    }
}