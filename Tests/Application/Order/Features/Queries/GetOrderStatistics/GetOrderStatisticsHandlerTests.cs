using Application.Order.Contracts;
using Application.Order.Features.Queries.GetOrderStatistics;
using Application.Order.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetOrderStatistics;

public class GetOrderStatisticsHandlerTests
{
    private readonly IOrderQueryService _orderQueryService = Substitute.For<IOrderQueryService>(); private readonly GetOrderStatisticsHandler _sut;

    public GetOrderStatisticsHandlerTests()
    {
        _sut = new GetOrderStatisticsHandler(_orderQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsStatisticsFromQueryService()
    {
        var stats = new OrderStatisticsDto { TotalOrders = 42, PendingOrders = 3, TotalRevenue = 12345m };
        _orderQueryService.GetOrderStatisticsAsync(Arg.Any<CancellationToken>()).Returns(stats);

        var result = await _sut.Handle(new GetOrderStatisticsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(stats);
        await _orderQueryService.Received(1).GetOrderStatisticsAsync(Arg.Any<CancellationToken>());
    }
}
