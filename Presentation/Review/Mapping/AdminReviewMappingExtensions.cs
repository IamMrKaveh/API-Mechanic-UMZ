using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;

namespace Presentation.Review.Mapping;

public static class AdminReviewMappingExtensions
{
    public static ApproveReviewCommand Enrich(
        this ApproveReviewCommand command,
        Guid reviewId) => command with { ReviewId = reviewId };

    public static DeleteReviewCommand Enrich(
        this DeleteReviewCommand command,
        Guid reviewId) => command with { ReviewId = reviewId };

    public static RejectReviewCommand Enrich(
        this RejectReviewCommand command,
        Guid reviewId) => command with { ReviewId = reviewId };
}