using Application.Review.Features.Commands.UpdateOwnReview;
using Presentation.Review.Mapping;

namespace Tests.Presentation.Review.Mapping;

public class ReviewMappingExtensionsTests
{
    [Fact]
    public void Enrich_SetsReviewId_AndKeepsValues()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var command = new UpdateOwnReviewCommand(Guid.Empty, 4, "Title", "Comment");

        // Act
        var enriched = command.Enrich(reviewId);

        // Assert
        enriched.ReviewId.ShouldBe(reviewId);
        enriched.Rating.ShouldBe(4);
        enriched.Title.ShouldBe("Title");
        enriched.Comment.ShouldBe("Comment");
    }

    [Fact]
    public void Enrich_DoesNotMutateOriginal()
    {
        // Arrange
        var command = new UpdateOwnReviewCommand(Guid.Empty, 4, null, null);

        // Act
        var enriched = command.Enrich(Guid.NewGuid());

        // Assert
        command.ReviewId.ShouldBe(Guid.Empty);
        enriched.ReviewId.ShouldNotBe(Guid.Empty);
    }
}
