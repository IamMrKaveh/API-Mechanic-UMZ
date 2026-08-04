using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.ReplyToReview;

public sealed class ReplyToReviewHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<ReplyToReviewCommand>
{
    public async Task<ServiceResult> Handle(
        ReplyToReviewCommand request,
        CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var review = await reviewRepository.GetByIdAsync(reviewId, ct);
        if (review is null)
            return ServiceResult.NotFound("نظر یافت نشد.");

        var previousStatus = review.Status.ToString();
        var previouslyHadReply = review.AdminReply is not null;

        review.AddAdminReply(request.Reply);
        reviewRepository.Update(review);

        auditContextEnricher.Set("previousStatus", previousStatus);
        auditContextEnricher.Set("newStatus", review.Status.ToString());
        auditContextEnricher.Set("previouslyHadReply", previouslyHadReply.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());
        auditContextEnricher.Set("replyLength", request.Reply.Length.ToString());

        return ServiceResult.Success();
    }
}
