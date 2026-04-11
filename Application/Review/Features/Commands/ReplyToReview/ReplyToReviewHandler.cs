using Domain.Common.Exceptions;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Commands.ReplyToReview;

public class ReplyToReviewHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReplyToReviewCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        ReplyToReviewCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);
        var userId = UserId.From(request.UserId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        try
        {
            review.AddAdminReply(request.Reply);

            reviewRepository.Update(review);
            await unitOfWork.SaveChangesAsync(ct);

            await auditService.LogProductEventAsync(
                review.ProductId,
                "ReplyToReview",
                $"Admin replied to review {request.ReviewId}.",
                userId);

            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            return ServiceResult.Failure(ex.Message);
        }
    }
}