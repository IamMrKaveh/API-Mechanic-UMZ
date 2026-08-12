using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetInventoryStatistics;
using Application.Inventory.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetInventoryStatistics;

public class GetInventoryStatisticsHandlerTests
{
    private readonly IInventoryQueryService _queryService = Substitute.For<IInventoryQueryService>(); private readonly GetInventoryStatisticsHandler _sut;

    public GetInventoryStatisticsHandlerTests()
    {
        _sut = new GetInventoryStatisticsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsNull_ReturnsNotFound()
    {
        _queryService
            .GetStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns((InventoryStatisticsDto?)null);

        var result = await _sut.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsStatistics_ReturnsSuccessWithSameValue()
    {
        var stats = new InventoryStatisticsDto
        {
            TotalVariants = 10,
            InStockVariants = 6,
            OutOfStockVariants = 2,
            LowStockVariants = 1,
            UnlimitedVariants = 1
        };

        _queryService
            .GetStatisticsAsync(Arg.Any<CancellationToken>())
            .Returns(stats);

        var result = await _sut.Handle(new GetInventoryStatisticsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(stats);
    }
}
