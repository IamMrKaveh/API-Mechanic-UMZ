using Application.Review.Features.Commands.RejectReview;
using Presentation.Review.Mapping;

namespace Tests.Presentation.Review.Mapping;

public class AdminReviewMappingExtensionsTests
{
    [Fact]
    public void Enrich_SetsReviewId_AndKeepsReason()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var command = new RejectReviewCommand(Guid.Empty, "spam");

        // Act
        var enriched = command.Enrich(reviewId);

        // Assert
        enriched.ReviewId.ShouldBe(reviewId);
        enriched.Reason.ShouldBe("spam");
    }

    [Fact]
    public void Enrich_DoesNotMutateOriginal()
    {
        // Arrange
        var command = new RejectReviewCommand(Guid.Empty, "spam");

        // Act
        var enriched = command.Enrich(Guid.NewGuid());

        // Assert
        command.ReviewId.ShouldBe(Guid.Empty);
        enriched.ReviewId.ShouldNotBe(Guid.Empty);
    }
}
