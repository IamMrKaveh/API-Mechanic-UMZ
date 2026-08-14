using Application.Order.Features.Commands.ExpireOrders;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.ExpireOrders;

public class ExpireOrdersHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly ExpireOrdersHandler _sut;

    public ExpireOrdersHandlerTests()
    {
        _sut = new ExpireOrdersHandler(_orderRepository);
    }

    [Fact]
    public async Task Handle_WhenNoPendingOrders_ReturnsZero()
    {
        _orderRepository.FindPendingExpiredAsync(Arg.Any<CancellationToken>()).Returns(new List<Orders>());

        var result = await _sut.Handle(new ExpireOrdersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(0);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenExpirableOrdersPresent_ExpiresEachAndReturnsCount()
    {
        var order1 = new OrderBuilder().Build();
        order1.MoveToPending();
        var order2 = new OrderBuilder().Build();
        order2.MoveToPending();

        _orderRepository.FindPendingExpiredAsync(Arg.Any<CancellationToken>()).Returns(new List<Orders> { order1, order2 });

        var result = await _sut.Handle(new ExpireOrdersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(2);
        order1.Status.ShouldBe(OrderStatusValue.Expired);
        order2.Status.ShouldBe(OrderStatusValue.Expired);
        _orderRepository.Received(1).Update(order1, Arg.Any<byte[]?>());
        _orderRepository.Received(1).Update(order2, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOneOrderCannotExpire_SkipsItAndCountsOnlyExpired()
    {
        var expirable = new OrderBuilder().Build();
        expirable.MoveToPending();

        var nonExpirable = new OrderBuilder().Build();
        nonExpirable.MoveToPending();
        nonExpirable.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));

        _orderRepository.FindPendingExpiredAsync(Arg.Any<CancellationToken>()).Returns(new List<Orders> { expirable, nonExpirable });

        var result = await _sut.Handle(new ExpireOrdersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(1);
        expirable.Status.ShouldBe(OrderStatusValue.Expired);
        nonExpirable.Status.ShouldBe(OrderStatusValue.Paid);
        _orderRepository.Received(1).Update(expirable, Arg.Any<byte[]?>());
        _orderRepository.DidNotReceive().Update(nonExpirable, Arg.Any<byte[]?>());
    }
}
