using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.ReplyToReview;

public sealed class ReplyToReviewHandler(
    IReviewRepository reviewRepository)
    : ICommandHandler<ReplyToReviewCommand>
{
    public async Task<ServiceResult> Handle(
        ReplyToReviewCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.AddAdminReply(request.Reply);
        reviewRepository.Update(review);

        return ServiceResult.Success();
    }
}
