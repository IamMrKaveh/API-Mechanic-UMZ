using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;

namespace Application.Review.Features.Commands.RemoveAdminReply;

public sealed class RemoveAdminReplyHandler(
    IReviewRepository reviewRepository,
    IAuditContextEnricher auditContextEnricher)
    : ICommandHandler<RemoveAdminReplyCommand>
{
    public async Task<ServiceResult> Handle(RemoveAdminReplyCommand request, CancellationToken ct)
    {
        var reviewId = ReviewId.From(request.ReviewId);

        var reviewResult = await (reviewRepository.GetByIdAsync(reviewId, ct)).OrNotFoundAsync("نظر یافت نشد.");
        if (reviewResult.IsFailure) return reviewResult.Error;
        var review = reviewResult.Value;

        var previousReplyLength = review.AdminReply?.Length ?? 0;
        var previouslyHadReply = review.AdminReply is not null;

        review.RemoveAdminReply();
        reviewRepository.Update(review);

        auditContextEnricher.Set("previouslyHadReply", previouslyHadReply.ToString());
        auditContextEnricher.Set("previousReplyLength", previousReplyLength.ToString());
        auditContextEnricher.Set("reviewId", review.Id.Value.ToString());
        auditContextEnricher.Set("productId", review.ProductId.Value.ToString());

        return ServiceResult.Success();
    }
}
