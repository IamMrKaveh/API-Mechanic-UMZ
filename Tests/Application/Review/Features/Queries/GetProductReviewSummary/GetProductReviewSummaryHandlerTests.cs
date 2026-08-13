using Application.Review.Contracts;
using Application.Review.Features.Queries.GetProductReviewSummary;
using Application.Review.Features.Shared;
using Domain.Product.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.GetProductReviewSummary;

public class GetProductReviewSummaryHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly GetProductReviewSummaryHandler _sut;

    public GetProductReviewSummaryHandlerTests()
    {
        _sut = new GetProductReviewSummaryHandler(
            _reviewQueryService,
            NullLogger<GetProductReviewSummaryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WhenSummaryIsNull_ReturnsNotFound()
    {
        _reviewQueryService
            .GetProductReviewSummaryAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns((ReviewSummaryDto?)null);

        var result = await _sut.Handle(
            new GetProductReviewSummaryQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenSummaryExists_ReturnsSuccessWithSummary()
    {
        var productId = Guid.NewGuid();
        var summary = new ReviewSummaryDto { ProductId = productId, TotalReviews = 4, AverageRating = 4.25 };

        _reviewQueryService
            .GetProductReviewSummaryAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(summary);

        var result = await _sut.Handle(
            new GetProductReviewSummaryQuery(productId),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(summary);
    }

    [Fact]
    public async Task Handle_PassesProductIdBuiltFromRequestToQueryService()
    {
        var productId = Guid.NewGuid();
        ProductId? captured = null;

        _reviewQueryService
            .GetProductReviewSummaryAsync(
                Arg.Do<ProductId>(p => captured = p),
                Arg.Any<CancellationToken>())
            .Returns((ReviewSummaryDto?)null);

        _ = await _sut.Handle(new GetProductReviewSummaryQuery(productId), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(productId);
    }
}
