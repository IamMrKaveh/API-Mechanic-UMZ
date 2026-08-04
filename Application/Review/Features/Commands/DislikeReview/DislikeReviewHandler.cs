using Application.Review.Configuration;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;

namespace Application.Review.Features.Commands.DislikeReview;

public sealed class DislikeReviewHandler(
    IReviewRepository reviewRepository,
    ICurrentUserService currentUserService,
    IOptions<ReviewSettings> settings)
    : ICommandHandler<DislikeReviewCommand>
{
    public async Task<ServiceResult> Handle(DislikeReviewCommand request, CancellationToken ct)
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
            review.AddDislike(userId);
            reviewRepository.Update(review);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(Error.Validation(ex.Message));
        }
    }
}
