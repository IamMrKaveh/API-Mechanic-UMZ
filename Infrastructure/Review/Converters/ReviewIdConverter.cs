using Domain.Review.ValueObjects;

namespace Infrastructure.Review.Converters;

internal sealed class ReviewIdConverter : StronglyTypedIdConverter<ReviewId>
{
    public ReviewIdConverter() : base(ReviewId.From)
    {
    }
}