using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateAdminReply;

public sealed class UpdateAdminReplyHandler(
    IReviewRepository reviewRepository)
    : ICommandHandler<UpdateAdminReplyCommand>
{
    public async Task<ServiceResult> Handle(UpdateAdminReplyCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.UpdateAdminReply(request.Reply);
        reviewRepository.Update(review);

        return ServiceResult.Success();
    }
}
