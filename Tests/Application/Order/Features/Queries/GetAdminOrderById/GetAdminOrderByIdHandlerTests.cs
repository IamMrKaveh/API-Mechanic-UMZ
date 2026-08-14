using Application.Order.Contracts;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetAdminOrderById;

public class GetAdminOrderByIdHandlerTests
{
    private readonly IOrderQueryService _orderQueryService = Substitute.For<IOrderQueryService>(); private readonly GetAdminOrderByIdHandler _sut;

    public GetAdminOrderByIdHandlerTests()
    {
        _sut = new GetAdminOrderByIdHandler(_orderQueryService);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderQueryService.GetAdminOrderDetailsAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((AdminOrderDto?)null);

        var result = await _sut.Handle(new GetAdminOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOrderFound_ReturnsSuccessWithDto()
    {
        var dto = new AdminOrderDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" };
        _orderQueryService.GetAdminOrderDetailsAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetAdminOrderByIdQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_PassesOrderIdBuiltFromRequestGuidToService()
    {
        var orderGuid = Guid.NewGuid();
        OrderId? captured = null;
        _orderQueryService
            .GetAdminOrderDetailsAsync(Arg.Do<OrderId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns(new AdminOrderDto());

        _ = await _sut.Handle(new GetAdminOrderByIdQuery(orderGuid), CancellationToken.None);

        captured!.Value.ShouldBe(orderGuid);
    }
}
