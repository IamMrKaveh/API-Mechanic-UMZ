using Application.Review.Contracts;
using Application.Review.Features.Queries.AdminReviewStats;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Review.Features.Queries.AdminReviewStats;

public class GetAdminReviewStatsHandlerTests
{
    private readonly IReviewQueryService _reviewQueryService = Substitute.For<IReviewQueryService>(); private readonly GetAdminReviewStatsHandler _sut;

    public GetAdminReviewStatsHandlerTests()
    {
        _sut = new GetAdminReviewStatsHandler(
            _reviewQueryService,
            NullLogger<GetAdminReviewStatsHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsStatsFromQueryService()
    {
        var stats = new AdminReviewStatsDto(3, 7, 2, 12);
        _reviewQueryService
            .GetAdminReviewStatsAsync(Arg.Any<CancellationToken>())
            .Returns(stats);

        var result = await _sut.Handle(
            new GetAdminReviewStatsQuery(),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(stats);
        await _reviewQueryService.Received(1).GetAdminReviewStatsAsync(Arg.Any<CancellationToken>());
    }
}
