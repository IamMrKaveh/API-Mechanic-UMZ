using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RejectReview;

public class RejectReviewHandler(
    IReviewRepository reviewRepository,
    IAuditService auditService)
    : ICommandHandler<RejectReviewCommand>
{
    public async Task<ServiceResult> Handle(RejectReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.Reject(request.Reason);
        reviewRepository.Update(review);

        await auditService.LogSystemEventAsync(
            "RejectReview",
            $"نظر {request.ReviewId} با دلیل \"{request.Reason}\" رد شد.",
            ct);

        return ServiceResult.Success();
    }
}