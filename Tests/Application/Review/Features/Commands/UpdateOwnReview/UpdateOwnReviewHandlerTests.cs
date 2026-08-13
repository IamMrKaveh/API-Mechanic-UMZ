using Application.Common.Interfaces;
using Application.Review.Features.Commands.UpdateOwnReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.UpdateOwnReview;

public class UpdateOwnReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly UpdateOwnReviewHandler _sut;

    public UpdateOwnReviewHandlerTests()
    {
        _sut = new UpdateOwnReviewHandler(_reviewRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(
            new UpdateOwnReviewCommand(Guid.NewGuid(), 5, "title", "comment"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _reviewRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenReviewNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns((ProductReview?)null);

        var result = await _sut.Handle(
            new UpdateOwnReviewCommand(Guid.NewGuid(), 5, "t", "c"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ReturnsForbiddenAndDoesNotUpdateContent()
    {
        var ownerId = UserId.NewId();
        var review = new ProductReviewBuilder()
            .WithUserId(ownerId)
            .WithRating(3)
            .WithTitle("original")
            .WithComment("original comment")
            .Build();

        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateOwnReviewCommand(review.Id.Value, 5, "new", "new comment"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        review.Rating.Value.ShouldBe(3);
        review.Title.ShouldBe("original");
        review.Comment.ShouldBe("original comment");
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCallerIsOwner_UpdatesContentAndPersistsReview()
    {
        var callerGuid = Guid.NewGuid();
        var ownerId = UserId.From(callerGuid);
        var review = new ProductReviewBuilder()
            .WithUserId(ownerId)
            .WithRating(3)
            .WithTitle("old title")
            .WithComment("old comment")
            .Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(
            new UpdateOwnReviewCommand(review.Id.Value, 5, "new title", "new comment"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        review.Rating.Value.ShouldBe(5);
        review.Title.ShouldBe("new title");
        review.Comment.ShouldBe("new comment");
        review.Status.Value.ShouldBe("Pending");
        _reviewRepository.Received(1).Update(review);
    }
}
