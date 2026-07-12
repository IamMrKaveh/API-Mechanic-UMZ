using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Commands.UpdateOwnReview;

public sealed class UpdateOwnReviewHandler(
    IReviewRepository reviewRepository,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateOwnReviewCommand>
{
    public async Task<ServiceResult> Handle(UpdateOwnReviewCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return ServiceResult.Unauthorized("برای ویرایش نظر ابتدا وارد شوید.");

        var reviewId = ReviewId.From(request.ReviewId);
        var userId = UserId.From(currentUser.UserId!.Value);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        if (review.UserId != userId)
            return ServiceResult.Forbidden("امکان ویرایش نظر دیگران وجود ندارد.");

        try
        {
            var rating = Rating.Create(request.Rating);
            review.UpdateContent(rating, request.Title, request.Comment);

            reviewRepository.Update(review);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}