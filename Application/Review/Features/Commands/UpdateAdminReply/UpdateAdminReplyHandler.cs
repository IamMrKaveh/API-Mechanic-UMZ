using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.UpdateAdminReply;

public sealed class UpdateAdminReplyHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<UpdateAdminReplyCommand>
{
    public async Task<ServiceResult> Handle(UpdateAdminReplyCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousReplyLength = review.AdminReply?.Length ?? 0;

        review.UpdateAdminReply(request.Reply);
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousReplyLength", previousReplyLength.ToString());
        auditContextEnricher.Set("newReplyLength", request.Reply.Length.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());

        return ServiceResult.Success();
    }
}
