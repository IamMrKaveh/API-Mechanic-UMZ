using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RemoveAdminReply;

public sealed class RemoveAdminReplyHandler(
    IReviewRepository reviewRepository)
    : ICommandHandler<RemoveAdminReplyCommand>
{
    public async Task<ServiceResult> Handle(RemoveAdminReplyCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.RemoveAdminReply();
        reviewRepository.Update(review);

        return ServiceResult.Success();
    }
}
