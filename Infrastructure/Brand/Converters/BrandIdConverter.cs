using Domain.Brand.ValueObjects;

namespace Infrastructure.Brand.Converters;

internal sealed class BrandIdConverter : StronglyTypedIdConverter<BrandId>
{
    public BrandIdConverter() : base(BrandId.From)
    {
    }
}