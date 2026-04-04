using Domain.Review.Aggregates;

namespace Domain.Review.Specifications;

public class PendingReviewsSpecification : Specification<ProductReview>
{
    public override Expression<Func<ProductReview, bool>> ToExpression()
    {
        return r => r.Status == ValueObjects.ReviewStatus.Pending && !r.IsDeleted;
    }
}