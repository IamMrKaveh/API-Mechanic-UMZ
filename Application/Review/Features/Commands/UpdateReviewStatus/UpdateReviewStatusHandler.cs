using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public sealed class UpdateReviewStatusHandler(
    IReviewRepository reviewRepository)
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
        return ServiceResult.Success();
    }
}
