using Application.Review.Features.Commands.UpdateOwnReview;

namespace Presentation.Review.Mapping;

public static class ReviewMappingExtensions
{
    public static UpdateOwnReviewCommand Enrich(
        this UpdateOwnReviewCommand command,
        Guid reviewId) => command with { ReviewId = reviewId };
}