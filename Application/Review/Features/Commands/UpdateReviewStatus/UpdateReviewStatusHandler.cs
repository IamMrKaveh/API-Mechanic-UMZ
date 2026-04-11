using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public class UpdateReviewStatusHandler(IReviewRepository reviewRepository, IUnitOfWork unitOfWork, ILogger<UpdateReviewStatusHandler> logger) : IRequestHandler<UpdateReviewStatusCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateReviewStatusCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
        {
            return ServiceResult.NotFound("Review not found");
        }

        if (request.Status == "Approved")
            review.Approve();
        else if (request.Status == "Rejected")
            review.Reject();

        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Review {ReviewId} status updated to {Status}", request.ReviewId, request.Status);
        return ServiceResult.Success();
    }
}