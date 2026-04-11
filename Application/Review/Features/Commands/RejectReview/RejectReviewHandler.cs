using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RejectReview;

public class RejectReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RejectReviewCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RejectReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.Reject(request.Reason);
        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}