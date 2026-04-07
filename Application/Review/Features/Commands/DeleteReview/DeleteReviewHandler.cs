using Application.Common.Results;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Review.Features.Commands.DeleteReview;

public class DeleteReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<DeleteReviewCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(DeleteReviewCommand request, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(ReviewId.From(request.ReviewId), ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.MarkAsDeleted();
        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}