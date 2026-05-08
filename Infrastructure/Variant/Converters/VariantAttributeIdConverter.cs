using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Converters;

internal sealed class VariantAttributeIdConverter : StronglyTypedIdConverter<VariantAttributeId>
{
    public VariantAttributeIdConverter() : base(VariantAttributeId.From)
    {
    }
}