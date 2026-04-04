using Domain.Review.Aggregates;

namespace Domain.Review.Specifications;

public class ProductWithMinRatingSpecification : Specification<ProductReview>
{
    private readonly int _minRating;

    public ProductWithMinRatingSpecification(int minRating)
    {
        if (minRating < 1 || minRating > 5)
            throw new DomainException("Minimum rating must be between 1 and 5.");

        _minRating = minRating;
    }

    public override Expression<Func<ProductReview, bool>> ToExpression()
    {
        return r => r.Rating >= _minRating
                    && r.Status == Domain.Review.ValueObjects.ReviewStatus.Approved
                    && !r.IsDeleted;
    }
}