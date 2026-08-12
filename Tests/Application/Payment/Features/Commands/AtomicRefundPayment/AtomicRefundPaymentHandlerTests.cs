using Application.Payment.Features.Commands.AtomicRefundPayment;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;
using PaymentTransactions = Domain.Payment.Aggregates.PaymentTransaction;

namespace Tests.Application.Payment.Features.Commands.AtomicRefundPayment;

public class AtomicRefundPaymentHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly IPaymentTransactionRepository _paymentRepository = Substitute.For<IPaymentTransactionRepository>(); private readonly AtomicRefundPaymentHandler _sut;

    public AtomicRefundPaymentHandlerTests()
    {
        _sut = new AtomicRefundPaymentHandler(_orderRepository, _paymentRepository);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Orders?)null);

        var result = await _sut.Handle(
            new AtomicRefundPaymentCommand(Guid.NewGuid(), "customer request"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _paymentRepository.DidNotReceiveWithAnyArgs().GetVerifiedByOrderIdAsync(default!, default);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
        _paymentRepository.DidNotReceive().Update(Arg.Any<PaymentTransactions>());
    }

    [Fact]
    public async Task Handle_WhenOrderIsNotPaid_ReturnsFailure()
    {
        var order = new OrderBuilder().Build();
        order.IsPaid.ShouldBeFalse();

        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _sut.Handle(
            new AtomicRefundPaymentCommand(order.Id.Value, "customer request"),
            CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        await _paymentRepository.DidNotReceiveWithAnyArgs().GetVerifiedByOrderIdAsync(default!, default);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
        _paymentRepository.DidNotReceive().Update(Arg.Any<PaymentTransactions>());
    }
}
