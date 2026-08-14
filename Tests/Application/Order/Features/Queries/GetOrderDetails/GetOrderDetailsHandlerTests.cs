using Application.Common.Interfaces;
using Application.Order.Contracts;
using Application.Order.Features.Queries.GetOrderDetails;
using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Queries.GetOrderDetails;

public class GetOrderDetailsHandlerTests
{
    private readonly IOrderQueryService _orderQueryService = Substitute.For<IOrderQueryService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetOrderDetailsHandler _sut;

    public GetOrderDetailsHandlerTests()
    {
        _sut = new GetOrderDetailsHandler(_orderQueryService, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetOrderDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orderQueryService.DidNotReceiveWithAnyArgs().GetOrderDetailsAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _orderQueryService.GetOrderDetailsAsync(Arg.Any<OrderId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns((OrderDto?)null);

        var result = await _sut.Handle(new GetOrderDetailsQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOrderFound_ReturnsSuccessWithDto()
    {
        var userGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)userGuid);
        var dto = new OrderDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" };
        _orderQueryService.GetOrderDetailsAsync(Arg.Any<OrderId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(dto);

        var result = await _sut.Handle(new GetOrderDetailsQuery(dto.Id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dto);
    }

    [Fact]
    public async Task Handle_PassesOrderIdAndUserIdBuiltFromRequestAndCurrentUserToService()
    {
        var userGuid = Guid.NewGuid();
        var orderGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)userGuid);

        OrderId? capturedOrder = null;
        UserId? capturedUser = null;
        _orderQueryService
            .GetOrderDetailsAsync(Arg.Do<OrderId>(o => capturedOrder = o), Arg.Do<UserId>(u => capturedUser = u), Arg.Any<CancellationToken>())
            .Returns(new OrderDto());

        _ = await _sut.Handle(new GetOrderDetailsQuery(orderGuid), CancellationToken.None);

        capturedOrder!.Value.ShouldBe(orderGuid);
        capturedUser!.Value.ShouldBe(userGuid);
    }
}
