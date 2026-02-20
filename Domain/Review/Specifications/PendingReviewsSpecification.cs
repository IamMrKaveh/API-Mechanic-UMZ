namespace Domain.Review.Specifications;

public class PendingReviewsSpecification : Specification<ProductReview>
{
    public override Expression<Func<ProductReview, bool>> ToExpression()
    {
        return r => r.Status == ProductReview.ReviewStatus.Pending && !r.IsDeleted;
    }
}