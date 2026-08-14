using Application.Review.Contracts;
using Application.Review.Features.Queries.GetReviewsByStatus;
using Application.Review.Features.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Models;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.GetReviewsByStatus;

public class GetReviewsByStatusHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly GetReviewsByStatusHandler _sut;

    public GetReviewsByStatusHandlerTests()
    {
        _sut = new GetReviewsByStatusHandler(
            _reviewQueryService,
            NullLogger<GetReviewsByStatusHandler>.Instance);
    }

    [Theory]
    [InlineData("Bogus")]
    [InlineData("Deleted")]
    [InlineData("Any")]
    public async Task Handle_WhenStatusNotInAllowedList_ReturnsValidationFailure(string status)
    {
        var result = await _sut.Handle(
            new GetReviewsByStatusQuery(status),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        await _reviewQueryService.DidNotReceiveWithAnyArgs()
            .GetReviewsByStatusAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenStatusIsWhitespace_UsesPendingAsDefaultAndSucceeds()
    {
        var expected = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetReviewsByStatusAsync(Arg.Any<AdminReviewFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetReviewsByStatusQuery("   "),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _reviewQueryService.Received(1)
            .GetReviewsByStatusAsync(
                Arg.Is<AdminReviewFilter>(f => f!.Status == "Pending"),
                Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("approved", "Approved")]
    [InlineData("REJECTED", "Rejected")]
    [InlineData("Pending", "Pending")]
    [InlineData("all", "All")]
    public async Task Handle_NormalizesStatusToCanonicalCasing(string input, string canonical)
    {
        var expected = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetReviewsByStatusAsync(Arg.Any<AdminReviewFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new GetReviewsByStatusQuery(input),
            CancellationToken.None);

        result.ShouldBeSuccess();
        await _reviewQueryService.Received(1)
            .GetReviewsByStatusAsync(
                Arg.Is<AdminReviewFilter>(f => f!.Status == canonical),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NormalizesWhitespaceOnlySearchTextToNull()
    {
        var expected = new PaginatedResult<ProductReviewDto>(Array.Empty<ProductReviewDto>(), 0, 1, 10);
        _reviewQueryService
            .GetReviewsByStatusAsync(Arg.Any<AdminReviewFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        _ = await _sut.Handle(
            new GetReviewsByStatusQuery("Approved", SearchText: "   "),
            CancellationToken.None);

        await _reviewQueryService.Received(1)
            .GetReviewsByStatusAsync(
                Arg.Is<AdminReviewFilter>(f => f!.SearchText == null),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForwardsAllFilterParametersToQueryService()
    {
        var expected = new PaginatedResult<ProductReviewDto>([], 0, 2, 25);
        _reviewQueryService
            .GetReviewsByStatusAsync(Arg.Any<AdminReviewFilter>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var productId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await _sut.Handle(
            new GetReviewsByStatusQuery(
                "Approved", 2, 25, "keyword", 4, productId, from, to),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(expected);
        await _reviewQueryService.Received(1)
            .GetReviewsByStatusAsync(
                Arg.Is<AdminReviewFilter>(f =>
                    f!.Status == "Approved" &&
                    f.Page == 2 &&
                    f.PageSize == 25 &&
                    f.SearchText == "keyword" &&
                    f.MinRating == 4 &&
                    f.ProductId == productId &&
                    f.DateFrom == from &&
                    f.DateTo == to),
                Arg.Any<CancellationToken>());
    }
}
