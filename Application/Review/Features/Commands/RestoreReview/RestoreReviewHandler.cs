using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RestoreReview;

public sealed class RestoreReviewHandler(
    IReviewRepository reviewRepository)
    : ICommandHandler<RestoreReviewCommand>
{
    public async Task<ServiceResult> Handle(RestoreReviewCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdIncludingDeletedAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.Restore();
        reviewRepository.Update(review);

        return ServiceResult.Success();
    }
}
