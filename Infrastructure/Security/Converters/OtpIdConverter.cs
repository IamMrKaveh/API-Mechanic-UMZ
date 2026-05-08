using Domain.Security.ValueObjects;

namespace Infrastructure.Security.Converters;

internal sealed class OtpIdConverter : StronglyTypedIdConverter<OtpId>
{
    public OtpIdConverter() : base(OtpId.From)
    {
    }
}