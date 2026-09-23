using Application.Review.Features.Shared;
using Application.Review.Mapping;
using Mapster;

namespace Tests.Application.Review.Mapping;

public class ReviewMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public ReviewMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new ReviewMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_ProductReview_ToDto_MapsScalarsAndUserFullName()
    {
        var review = new ProductReviewBuilder()
            .WithRating(5)
            .WithTitle("Great pads")
            .WithComment("Very good")
            .BuildApproved();

        var dto = _mapper.Map<ProductReviewDto>(review);

        dto.Id.ShouldBe(review.Id.Value);
        dto.ProductId.ShouldBe(review.ProductId.Value);
        dto.UserId.ShouldBe(review.UserId.Value);
        dto.OrderId.ShouldBe(review.OrderId!.Value);
        dto.Rating.ShouldBe(5);
        dto.Title.ShouldBe("Great pads");
        dto.Comment.ShouldBe("Very good");
        dto.Status.ShouldBe(review.Status.Value);
        dto.IsVerifiedPurchase.ShouldBe(review.IsVerifiedPurchase);
        dto.LikeCount.ShouldBe(review.LikeCount);
        dto.DislikeCount.ShouldBe(review.DislikeCount);
        dto.CreatedAt.ShouldBe(review.CreatedAt);
        dto.UserFullName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Map_ProductReview_WithoutOrder_MapsNullOrderId()
    {
        var review = new ProductReviewBuilder().WithoutOrderId().Build();

        var dto = _mapper.Map<ProductReviewDto>(review);

        dto.OrderId.ShouldBeNull();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new ReviewMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
