using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RestoreReview;

public sealed class RestoreReviewHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<RestoreReviewCommand>
{
    public async Task<ServiceResult> Handle(RestoreReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdIncludingDeletedAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousIsDeleted = review.IsDeleted;

        review.Restore();
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousIsDeleted", previousIsDeleted.ToString());
        auditContextEnricher.Set("newIsDeleted", review.IsDeleted.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());
        auditContextEnricher.Set("ownerUserId", review.UserId.Value.ToString());

        return ServiceResult.Success();
    }
}
