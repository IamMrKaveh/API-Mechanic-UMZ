using Domain.Attribute.ValueObjects;

namespace Infrastructure.Attribute.Converters;

internal sealed class AttributeTypeIdConverter
    : StronglyTypedIdConverter<AttributeTypeId>
{
    public AttributeTypeIdConverter() : base(AttributeTypeId.From)
    {
    }
}