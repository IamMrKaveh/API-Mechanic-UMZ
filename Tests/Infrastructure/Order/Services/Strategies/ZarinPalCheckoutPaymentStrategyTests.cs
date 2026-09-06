using Application.Order.Features.Shared;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Aggregates;
using Domain.Order.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Order.Services.Strategies;
using SharedKernel.Abstractions.Interfaces;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services.Strategies;

public class ZarinPalCheckoutPaymentStrategyTests
{
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IPaymentTransactionRepository _paymentTransactionRepository = Substitute.For<IPaymentTransactionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly ZarinPalCheckoutPaymentStrategy _sut;

    private static readonly DateTime FixedNow = new(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

    public ZarinPalCheckoutPaymentStrategyTests()
    {
        _dateTimeProvider.UtcNow.Returns(FixedNow);
        _sut = new ZarinPalCheckoutPaymentStrategy(
            _paymentService,
            _orderRepository,
            _paymentTransactionRepository,
            _unitOfWork,
            _dateTimeProvider);
    }

    private static CheckoutResultDto NewOrderResult(Guid? orderId = null, decimal finalAmount = 150_000m) => new()
    {
        OrderId = orderId ?? Guid.NewGuid(),
        OrderNumber = "ON-3003",
        FinalAmount = finalAmount
    };

    private static global::Domain.Order.Aggregates.Order NewOrder(UserId? userId = null)
    {
        var order = new OrderBuilder().WithUserId(userId ?? UserId.NewId()).Build();
        order.ClearDomainEvents();
        return order;
    }

    [Fact]
    public void Code_IsZarinpal()
    {
        _sut.Code.ShouldBe(PaymentMethodCode.Zarinpal);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAmountIsZero_SettlesFreeOrder()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        var orderResult = NewOrderResult(order.Id.Value, finalAmount: 0m);
        var idempotencyKey = Guid.NewGuid();

        var result = await _sut.ExecuteAsync(
            orderResult, order.Id, userId, Money.FromDecimal(0m),
            "127.0.0.1", null, idempotencyKey, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        result.Value.PaymentAuthority.ShouldBe($"FREE-ZP-{idempotencyKey:N}");
        result.Value.PaymentTransactionId.ShouldNotBeNull();
        result.Value.PaymentMethodCode.ShouldBe(PaymentMethodCode.Zarinpal);
        order.IsPaid.ShouldBeTrue();
        await _paymentService.DidNotReceiveWithAnyArgs().InitiatePaymentAsync(
            default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPaymentInitiated_ReturnsPaymentUrlAndAuthority()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var orderResult = NewOrderResult(order.Id.Value);
        var transactionId = Guid.NewGuid();
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("AUTH-999", "https://pay.example/999", transactionId));

        var result = await _sut.ExecuteAsync(
            orderResult, order.Id, userId, Money.FromDecimal(150_000m),
            "127.0.0.1", "agent", Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.PaymentUrl.ShouldBe("https://pay.example/999");
        result.Value.PaymentAuthority.ShouldBe("AUTH-999");
        result.Value.PaymentTransactionId.ShouldBe(transactionId);
        result.Value.IsPaid.ShouldBeFalse();
        result.Value.PaymentMethodCode.ShouldBe(PaymentMethodCode.Zarinpal);
        result.Value.OrderId.ShouldBe(orderResult.OrderId);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsGatewayCodeAndClientIp()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("A", "https://pay.example/a", Guid.NewGuid()));

        await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "10.0.0.5", null, Guid.NewGuid(), CancellationToken.None);

        await _paymentService.Received(1).InitiatePaymentAsync(
            Arg.Is<OrderId>(id => id == order.Id),
            Arg.Is<Money>(m => m.Amount == 150_000m),
            Arg.Is<IpAddress>(ip => ip.Value == "10.0.0.5"),
            Arg.Is<UserId>(u => u == userId),
            PaymentMethodCode.Zarinpal,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenIpAddressIsBlank_UsesFallbackAddress()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("A", "https://pay.example/a", Guid.NewGuid()));

        await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, userId, Money.FromDecimal(150_000m),
            "   ", null, Guid.NewGuid(), CancellationToken.None);

        await _paymentService.Received(1).InitiatePaymentAsync(
            Arg.Any<OrderId>(), Arg.Any<Money>(),
            Arg.Is<IpAddress>(ip => ip.Value == "0.0.0.0"),
            Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderNotFoundInPaymentService_ReturnsNotFound()
    {
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(
                new OrderNotFoundException(OrderId.NewId())));

        var result = await _sut.ExecuteAsync(
            NewOrderResult(), OrderId.NewId(), UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderAlreadyPaid_ReturnsConflict()
    {
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(
                new OrderAlreadyPaidException(OrderId.NewId())));

        var result = await _sut.ExecuteAsync(
            NewOrderResult(), OrderId.NewId(), UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGatewayFails_ReturnsFailure()
    {
        _paymentService.InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(
                new ExternalServiceException("Zarinpal", "gateway down")));

        var result = await _sut.ExecuteAsync(
            NewOrderResult(), OrderId.NewId(), UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFreeOrderAlreadyPaid_ReturnsPaidResult()
    {
        var userId = UserId.NewId();
        var order = NewOrder(userId);
        var paidTx = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(userId)
            .Build();
        paidTx.MarkAsSuccess(222L, FixedNow);
        paidTx.ClearDomainEvents();
        order.MarkAsPaid(paidTx.Id);
        order.ClearDomainEvents();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value, finalAmount: 0m), order.Id, userId, Money.FromDecimal(0m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsPaid.ShouldBeTrue();
        result.Value.PaymentTransactionId.ShouldBe(paidTx.Id.Value);
        await _paymentService.DidNotReceiveWithAnyArgs().InitiatePaymentAsync(
            default!, default!, default!, default!, default, default);
    }
}
