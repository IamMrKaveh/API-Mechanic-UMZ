using Application.Common.Interfaces;
using Application.Review.Configuration;
using Application.Review.Features.Commands.LikeReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.LikeReview;

public class LikeReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private static IOptions<ReviewSettings> EnabledSettings()
        => Options.Create(new ReviewSettings { EnableLikeDislike = true });

    private static IOptions<ReviewSettings> DisabledSettings()
        => Options.Create(new ReviewSettings { EnableLikeDislike = false });

    [Fact]
    public async Task Handle_WhenFeatureDisabled_ReturnsValidationFailure()
    {
        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, DisabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _reviewRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, EnabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsEmptyGuid_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)Guid.Empty);
        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, EnabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, EnabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenDomainThrowsBecauseReviewNotApproved_ReturnsValidationFailure()
    {
        var voterGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)voterGuid);

        var review = new ProductReviewBuilder()
            .WithUserId(UserId.NewId())
            .Build();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, EnabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenReviewApprovedAndVoterIsNotOwner_AddsLikeAndUpdatesRepository()
    {
        var voterGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)voterGuid);

        var review = new ProductReviewBuilder()
            .WithUserId(UserId.NewId())
            .BuildApproved();

        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var sut = new LikeReviewHandler(_reviewRepository, _currentUser, EnabledSettings());

        var result = await sut.Handle(new LikeReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        review.LikeCount.ShouldBe(1);
        review.DislikeCount.ShouldBe(0);
        _reviewRepository.Received(1).Update(review);
    }
}
