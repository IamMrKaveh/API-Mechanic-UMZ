using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.DeleteReview;

public sealed class DeleteReviewHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<DeleteReviewCommand>
{
    public async Task<ServiceResult> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousStatus = review.Status.ToString();
        var previousIsDeleted = review.IsDeleted;

        review.MarkAsDeleted();
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousStatus", previousStatus);
        auditContextEnricher.Set("previousIsDeleted", previousIsDeleted.ToString());
        auditContextEnricher.Set("newIsDeleted", review.IsDeleted.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());
        auditContextEnricher.Set("ownerUserId", review.UserId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(request.Reason))
            auditContextEnricher.Set("reason", TruncateReason(request.Reason));

        return ServiceResult.Success();
    }

    private static string TruncateReason(string reason)
        => reason.Length <= 200 ? reason : reason[..200] + "…";
}
