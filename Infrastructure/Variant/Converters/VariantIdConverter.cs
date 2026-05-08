using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.Converters;

internal sealed class VariantIdConverter : StronglyTypedIdConverter<VariantId>
{
    public VariantIdConverter() : base(VariantId.From)
    {
    }
}