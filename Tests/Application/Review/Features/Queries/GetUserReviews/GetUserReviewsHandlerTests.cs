using Application.Common.Interfaces;
using Application.Review.Contracts;
using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Models;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.GetUserReviews;

public class GetUserReviewsHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetUserReviewsHandler _sut;

    public GetUserReviewsHandlerTests()
    {
        _sut = new GetUserReviewsHandler(
            _reviewQueryService,
            _currentUser,
            NullLogger<GetUserReviewsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAnonymous_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetUserReviewsQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _reviewQueryService.DidNotReceiveWithAnyArgs()
            .GetUserReviewsAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenNonAdminRequestsAnotherUsersReviews_ReturnsForbidden()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);

        var otherUserId = Guid.NewGuid();

        var result = await _sut.Handle(
            new GetUserReviewsQuery(1, 10, otherUserId),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _reviewQueryService.DidNotReceiveWithAnyArgs()
            .GetUserReviewsAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserRequestsOwnReviews_ReturnsSuccessAndUsesCurrentUserId()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);

        var page = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetUserReviewsAsync(Arg.Any<UserId>(), 1, 10, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(new GetUserReviewsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _reviewQueryService.Received(1)
            .GetUserReviewsAsync(
                Arg.Is<UserId>(u => u == UserId.From(callerGuid)),
                1,
                10,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAdminRequestsOtherUsersReviews_ReturnsSuccessAndUsesRequestedUserId()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(true);

        var otherUserId = Guid.NewGuid();

        var page = new PaginatedResult<ProductReviewDto>([], 0, 1, 10);
        _reviewQueryService
            .GetUserReviewsAsync(Arg.Any<UserId>(), 1, 10, Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetUserReviewsQuery(1, 10, otherUserId),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _reviewQueryService.Received(1)
            .GetUserReviewsAsync(
                Arg.Is<UserId>(u => u == UserId.From(otherUserId)),
                1,
                10,
                Arg.Any<CancellationToken>());
    }
}
