using Domain.Category.ValueObjects;

namespace Infrastructure.Category.Converters;

internal sealed class CategoryIdConverter : StronglyTypedIdConverter<CategoryId>
{
    public CategoryIdConverter() : base(CategoryId.From)
    {
    }
}