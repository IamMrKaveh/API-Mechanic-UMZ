using Application.Review.Features.Commands.CreateReview;

namespace Presentation.Review.Mapping;

public static class ReviewMappingExtensions
{
    public static CreateReviewCommand Enrich(
        this CreateReviewCommand command,
        Guid userId) => command with
        {
            UserId = userId
        };
}