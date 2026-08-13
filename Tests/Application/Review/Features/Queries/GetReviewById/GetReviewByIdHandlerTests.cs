using Application.Common.Interfaces;
using Application.Review.Contracts;
using Application.Review.Features.Queries.GetReviewById;
using Application.Review.Features.Shared;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.GetReviewById;

public class GetReviewByIdHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetReviewByIdHandler _sut;

    public GetReviewByIdHandlerTests()
    {
        _sut = new GetReviewByIdHandler(
            _reviewQueryService,
            _currentUser,
            NullLogger<GetReviewByIdHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsNotFound()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        _reviewQueryService
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns((ProductReviewDto?)null);

        var result = await _sut.Handle(
            new GetReviewByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAnonymous_PassesNullCurrentUserIdToQueryService()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var dto = new ProductReviewDto { Id = Guid.NewGuid() };
        _reviewQueryService
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(
            new GetReviewByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _reviewQueryService.Received(1)
            .GetByIdAsync(
                Arg.Any<ReviewId>(),
                Arg.Is<UserId?>(u => u == null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_PassesCurrentUserIdToQueryService()
    {
        var userGuid = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userGuid);

        var dto = new ProductReviewDto { Id = Guid.NewGuid() };
        _reviewQueryService
            .GetByIdAsync(Arg.Any<ReviewId>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(dto);

        var result = await _sut.Handle(
            new GetReviewByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(dto);

        await _reviewQueryService.Received(1)
            .GetByIdAsync(
                Arg.Any<ReviewId>(),
                Arg.Is<UserId?>(u => u != null && u.Value == userGuid),
                Arg.Any<CancellationToken>());
    }
}
