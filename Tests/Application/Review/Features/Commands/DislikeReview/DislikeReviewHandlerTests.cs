using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Review.Features.Commands.DislikeReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.DislikeReview;

public class DislikeReviewHandlerTests : HandlerTestBase
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private static IOptions<ReviewSettings> Enabled()
        => Options.Create(new ReviewSettings { EnableLikeDislike = true });

    [Fact]
    public async Task Handle_WhenFeatureDisabled_ReturnsValidationFailure()
    {
        var sut = new DislikeReviewHandler(
            _reviewRepository,
            CurrentUserService,
            Options.Create(new ReviewSettings { EnableLikeDislike = false }));

        var result = await sut.Handle(new DislikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ReturnsUnauthorized()
    {
        CurrentUserService.UserId.Returns((Guid?)null);
        var sut = new DislikeReviewHandler(_reviewRepository, CurrentUserService, Enabled());

        var result = await sut.Handle(new DislikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var sut = new DislikeReviewHandler(_reviewRepository, CurrentUserService, Enabled());

        var result = await sut.Handle(new DislikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenReviewApprovedAndVoterIsNotOwner_AddsDislikeAndUpdatesRepository()
    {
        var voterGuid = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)voterGuid);

        var review = new ProductReviewBuilder()
            .WithUserId(UserId.NewId())
            .BuildApproved();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var sut = new DislikeReviewHandler(_reviewRepository, CurrentUserService, Enabled());

        var result = await sut.Handle(new DislikeReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        review.DislikeCount.ShouldBe(1);
        review.LikeCount.ShouldBe(0);
        _reviewRepository.Received(1).Update(review);
    }
}
