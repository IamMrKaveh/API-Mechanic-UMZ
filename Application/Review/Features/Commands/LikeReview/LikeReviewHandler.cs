using Application.Review.Configuration;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;

namespace Application.Review.Features.Commands.LikeReview;

public sealed class LikeReviewHandler(
    IReviewRepository reviewRepository,
    ICurrentUserService currentUserService,
    IOptions<ReviewSettings> settings)
    : ICommandHandler<LikeReviewCommand>
{
    public async Task<ServiceResult> Handle(LikeReviewCommand request, CancellationToken ct)
    {
        if (!settings.Value.EnableLikeDislike)
            return ServiceResult.Failure(
                Error.Validation("قابلیت رأی‌گیری روی نظرات در حال حاضر غیرفعال است."));

        if (currentUserService.UserId is null || currentUserService.UserId.Value == Guid.Empty)
            return ServiceResult.Unauthorized("برای رأی دادن باید وارد شوید.");

        var reviewId = ReviewId.From(request.ReviewId);
        var userId = UserId.From(currentUserService.UserId.Value);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.AddLike(userId);
            reviewRepository.Update(review);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(Error.Validation(ex.Message));
        }
    }
}
