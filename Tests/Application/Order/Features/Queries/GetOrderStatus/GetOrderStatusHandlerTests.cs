using Application.Order.Contracts;
using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetOrderStatus;

public class GetOrderStatusHandlerTests
{
    private readonly IOrderStatusQueryService _orderStatusQueryService = Substitute.For<IOrderStatusQueryService>(); private readonly GetOrderStatusHandler _sut;

    public GetOrderStatusHandlerTests()
    {
        _sut = new GetOrderStatusHandler(_orderStatusQueryService);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _orderStatusQueryService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((OrderStatusDto?)null);

        var result = await _sut.Handle(new GetOrderStatusQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenFound_ReturnsSuccessWithDto()
    {
        var dto = new OrderStatusDto { Id = Guid.NewGuid(), Name = "paid" };
        _orderStatusQueryService.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetOrderStatusQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_PassesQueryIdToService()
    {
        var id = Guid.NewGuid();
        Guid capturedId = default;
        _orderStatusQueryService
            .GetByIdAsync(Arg.Do<Guid>(g => capturedId = g), Arg.Any<CancellationToken>())
            .Returns(new OrderStatusDto());

        _ = await _sut.Handle(new GetOrderStatusQuery(id), CancellationToken.None);

        capturedId.ShouldBe(id);
    }
}
