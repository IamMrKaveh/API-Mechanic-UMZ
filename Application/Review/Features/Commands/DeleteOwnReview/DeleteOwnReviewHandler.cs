using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Commands.DeleteOwnReview;

public sealed class DeleteOwnReviewHandler(
    IReviewRepository reviewRepository,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteOwnReviewCommand>
{
    public async Task<ServiceResult> Handle(DeleteOwnReviewCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return ServiceResult.Unauthorized("برای حذف نظر ابتدا وارد شوید.");

        var reviewId = ReviewId.From(request.ReviewId);
        var userId = UserId.From(currentUser.UserId!.Value);

        var reviewResult = await (reviewRepository.GetByIdAsync(reviewId, ct)).OrNotFoundAsync("نظر یافت نشد.");
        if (reviewResult.IsFailure) return reviewResult.Error;
        var review = reviewResult.Value;

        if (review.UserId != userId)
            return ServiceResult.Forbidden("امکان حذف نظر دیگران وجود ندارد.");

        review.MarkAsDeleted();
        reviewRepository.Update(review);

        return ServiceResult.Success();
    }
}