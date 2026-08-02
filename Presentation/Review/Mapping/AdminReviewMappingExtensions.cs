using Application.Review.Features.Commands.RejectReview;

namespace Presentation.Review.Mapping;

public static class AdminReviewMappingExtensions
{
    public static RejectReviewCommand Enrich(
        this RejectReviewCommand command,
        Guid reviewId) => command with { ReviewId = reviewId };
}
