using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public sealed class UpdateReviewStatusHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<UpdateReviewStatusCommand>
{
    public async Task<ServiceResult> Handle(
        UpdateReviewStatusCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousStatus = review.Status.ToString();

        if (string.Equals(request.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            review.Approve();
        }
        else if (string.Equals(request.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
        {
            review.Reject(request.Reason!);
        }
        else
        {
            return ServiceResult.Validation(
                $"وضعیت '{request.Status}' نامعتبر است. مقادیر مجاز: Approved، Rejected.");
        }

        reviewRepository.Update(review);

        auditContextEnricher.Set("previousStatus", previousStatus);
        auditContextEnricher.Set("newStatus", review.Status.ToString());
        auditContextEnricher.Set("requestedStatus", request.Status);
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());

        if (!string.IsNullOrWhiteSpace(request.Reason))
            auditContextEnricher.Set("reason", TruncateReason(request.Reason!));

        return ServiceResult.Success();
    }

    private static string TruncateReason(string reason)
        => reason.Length <= 200 ? reason : reason[..200] + "…";
}
