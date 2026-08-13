using Application.Common.Interfaces;
using Application.Review.Features.Commands.DeleteOwnReview;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Review.Features.Commands.DeleteOwnReview;

public class DeleteOwnReviewHandlerTests
{
    private readonly IReviewRepository _reviewRepository = Substitute.For<IReviewRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly DeleteOwnReviewHandler _sut;

    public DeleteOwnReviewHandlerTests()
    {
        _sut = new DeleteOwnReviewHandler(_reviewRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsNull_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new DeleteOwnReviewCommand(Guid.NewGuid()), CancellationToken.None);

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

        var result = await _sut.Handle(new DeleteOwnReviewCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwner_ReturnsForbiddenAndDoesNotUpdate()
    {
        var ownerId = UserId.NewId();
        var callerId = Guid.NewGuid();
        var review = new ProductReviewBuilder().WithUserId(ownerId).Build();

        _currentUser.UserId.Returns((Guid?)callerId);
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(new DeleteOwnReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        review.IsDeleted.ShouldBeFalse();
        _reviewRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCallerIsOwner_MarksReviewAsDeletedAndUpdatesRepository()
    {
        var callerGuid = Guid.NewGuid();
        var ownerId = UserId.From(callerGuid);
        var review = new ProductReviewBuilder().WithUserId(ownerId).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _reviewRepository
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<CancellationToken>())
            .Returns(review);

        var result = await _sut.Handle(new DeleteOwnReviewCommand(review.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        review.IsDeleted.ShouldBeTrue();
        _reviewRepository.Received(1).Update(review);
    }
}
