using Application.Order.Contracts;
using Application.Order.Features.Queries.GetOrderStatuses;
using Application.Order.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetOrderStatuses;

public class GetOrderStatusesHandlerTests
{
    private readonly IOrderStatusQueryService _orderStatusQueryService = Substitute.For<IOrderStatusQueryService>(); private readonly GetOrderStatusesHandler _sut;

    public GetOrderStatusesHandlerTests()
    {
        _sut = new GetOrderStatusesHandler(_orderStatusQueryService);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public async Task Handle_PropagatesOnlyActiveFilterToService(bool? onlyActive)
    {
        IReadOnlyList<OrderStatusDto> expected = new List<OrderStatusDto> { new(), new() };
        bool? captured = false;
        _orderStatusQueryService
            .GetAllAsync(Arg.Do<bool?>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetOrderStatusesQuery(onlyActive), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        captured.ShouldBe(onlyActive);
    }

    [Fact]
    public void CacheKey_ReflectsOnlyActiveArgument()
    {
        new GetOrderStatusesQuery(true).CacheKey.ShouldBe("order-status:list:onlyActive=True");
        new GetOrderStatusesQuery(false).CacheKey.ShouldBe("order-status:list:onlyActive=False");
        new GetOrderStatusesQuery(null).CacheKey.ShouldBe("order-status:list:onlyActive=all");
    }
}
