using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<ApproveReviewCommand>
{
    public async Task<ServiceResult> Handle(ApproveReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousStatus = review.Status.ToString();

        review.Approve();
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousStatus", previousStatus);
        auditContextEnricher.Set("newStatus", review.Status.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());

        return ServiceResult.Success();
    }
}
