using Application.Review.Features.Shared;

namespace Tests.Application.Review.Features.Shared;

public class ReviewDtosTests
{
    [Fact]
    public void ProductReviewDto_Defaults_AreEmpty()
    {
        var dto = new ProductReviewDto();

        dto.Id.ShouldBe(default(Guid));
        dto.UserFullName.ShouldBe(string.Empty);
        dto.Rating.ShouldBe(0);
        dto.Title.ShouldBeNull();
        dto.Comment.ShouldBeNull();
        dto.Status.ShouldBe(string.Empty);
        dto.UserVote.ShouldBeNull();
        dto.AdminReply.ShouldBeNull();
        dto.OrderId.ShouldBeNull();
    }

    [Fact]
    public void ProductReviewDto_RoundTrip()
    {
        var dto = new ProductReviewDto
        {
            Id = Guid.NewGuid(), ProductId = Guid.NewGuid(), UserId = Guid.NewGuid(),
            UserFullName = "Ali Rezaei", Rating = 5, Title = "Great",
            Comment = "Excellent pads", Status = "Approved",
            IsVerifiedPurchase = true, LikeCount = 10, DislikeCount = 1,
            UserVote = "Like", AdminReply = "Thanks", RepliedAt = new DateTime(2026, 1, 5),
            CreatedAt = new DateTime(2026, 1, 1), OrderId = Guid.NewGuid()
        };

        dto.Rating.ShouldBe(5);
        dto.UserVote.ShouldBe("Like");
        dto.OrderId.ShouldNotBeNull();
    }

    [Fact]
    public void ReviewSummaryDto_DefaultDistribution_IsEmpty()
    {
        var dto = new ReviewSummaryDto();

        dto.AverageRating.ShouldBe(0);
        dto.RatingDistribution.ShouldNotBeNull();
        dto.RatingDistribution.ShouldBeEmpty();
    }

    [Fact]
    public void ReviewSummaryDto_RoundTrip()
    {
        var dto = new ReviewSummaryDto
        {
            ProductId = Guid.NewGuid(), AverageRating = 4.2, TotalReviews = 10,
            TotalCount = 10, FiveStarCount = 6, FourStarCount = 2,
            ThreeStarCount = 1, TwoStarCount = 1, OneStarCount = 0,
            RatingDistribution = new Dictionary<int, int> { [5] = 6, [4] = 2, [3] = 1, [2] = 1, [1] = 0 }
        };

        dto.AverageRating.ShouldBe(4.2);
        dto.RatingDistribution[5].ShouldBe(6);
    }
}
