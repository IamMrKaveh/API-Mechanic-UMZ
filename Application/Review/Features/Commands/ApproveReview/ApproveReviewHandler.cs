using Application.Common.Results;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Review.Features.Commands.ApproveReview;

public class ApproveReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ApproveReviewCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ApproveReviewCommand request, CancellationToken ct)
    {
        var review = await reviewRepository.GetByIdAsync(ProductReviewId.From(request.ReviewId), ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        review.Approve();
        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}