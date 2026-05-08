using Domain.Attribute.ValueObjects;

namespace Infrastructure.Attribute.Converters;

internal sealed class AttributeValueIdConverter : StronglyTypedIdConverter<AttributeValueId>
{
    public AttributeValueIdConverter() : base(AttributeValueId.From)
    {
    }
}