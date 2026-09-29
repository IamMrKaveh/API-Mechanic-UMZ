using Application.Common.Interfaces;
using Application.Review.Contracts;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Models;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.GetProductReviews;

public class GetProductReviewsHandlerTests : HandlerTestBase
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly GetProductReviewsHandler _sut;

    public GetProductReviewsHandlerTests()
    {
        _sut = new GetProductReviewsHandler(
            _reviewQueryService,
            CurrentUserService,
            NullLogger<GetProductReviewsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenAnonymous_PassesNullCurrentUserIdToQueryService()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var page = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetApprovedProductReviewsAsync(
                Arg.Any<ProductId>(), 1, 10, "Newest", null, false,
                Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetProductReviewsQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();

        await _reviewQueryService.Received(1)
            .GetApprovedProductReviewsAsync(
                Arg.Any<ProductId>(),
                1,
                10,
                "Newest",
                null,
                false,
                Arg.Is<UserId?>(u => u == null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_PassesCurrentUserIdToQueryService()
    {
        var userGuid = Guid.NewGuid();
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)userGuid);

        var page = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetApprovedProductReviewsAsync(
                Arg.Any<ProductId>(), 1, 10, "Newest", null, false,
                Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(page);

        var result = await _sut.Handle(
            new GetProductReviewsQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();

        await _reviewQueryService.Received(1)
            .GetApprovedProductReviewsAsync(
                Arg.Any<ProductId>(),
                1,
                10,
                "Newest",
                null,
                false,
                Arg.Is<UserId?>(u => u != null && u.Value == userGuid),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesQueryOptionsFromRequestToQueryService()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var expected = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 2, 25);
        _reviewQueryService
            .GetApprovedProductReviewsAsync(
                Arg.Any<ProductId>(), 2, 25, "Rating", 4, true,
                Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetProductReviewsQuery(Guid.NewGuid(), 2, 25, "Rating", 4, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
    }
}
