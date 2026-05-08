using Domain.Product.ValueObjects;

namespace Infrastructure.Product.Converters;

internal sealed class ProductIdConverter : StronglyTypedIdConverter<ProductId>
{
    public ProductIdConverter() : base(ProductId.From)
    {
    }
}