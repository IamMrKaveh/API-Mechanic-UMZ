using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateReviewStatus;

public sealed class UpdateReviewStatusHandler(
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateReviewStatusCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateReviewStatusCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        if (request.Status == "Approved")
            review.Approve();
        else if (request.Status == "Rejected")
            review.Reject();

        reviewRepository.Update(review);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogSystemEventAsync(
            "UpdateReviewStatus",
            $"وضعیت نظر {request.ReviewId} به '{request.Status}' تغییر کرد.",
            ct);

        return ServiceResult.Success();
    }
}