using Domain.Media.ValueObjects;

namespace Infrastructure.Media.Converters;

internal sealed class MediaIdConverter : StronglyTypedIdConverter<MediaId>
{
    public MediaIdConverter() : base(MediaId.From)
    {
    }
}