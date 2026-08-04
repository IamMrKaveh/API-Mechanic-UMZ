using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RejectReview;

public sealed class RejectReviewHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<RejectReviewCommand>
{
    public async Task<ServiceResult> Handle(RejectReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousStatus = review.Status.ToString();

        review.Reject(request.Reason);
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousStatus", previousStatus);
        auditContextEnricher.Set("newStatus", review.Status.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());
        auditContextEnricher.Set("reason", TruncateReason(request.Reason));

        return ServiceResult.Success();
    }

    private static string TruncateReason(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return string.Empty;
        return reason.Length <= 200 ? reason : reason[..200] + "…";
    }
}
