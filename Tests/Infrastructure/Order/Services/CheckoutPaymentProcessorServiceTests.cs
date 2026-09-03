using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using Application.Payment.Contracts;
using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Infrastructure.Order.Services;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutPaymentProcessorServiceTests
{
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>();
    private readonly CheckoutPaymentProcessorService _sut;

    public CheckoutPaymentProcessorServiceTests()
    {
        _sut = new CheckoutPaymentProcessorService(_paymentService);
    }

    private static CheckoutResultDto NewOrderResult(decimal finalAmount) => new()
    {
        OrderId = Guid.NewGuid(),
        OrderNumber = "ON-1001",
        FinalAmount = finalAmount
    };

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task ProcessAsync_WhenFinalAmountIsZeroOrNegative_SkipsPayment(decimal finalAmount)
    {
        var orderResult = NewOrderResult(finalAmount);

        var result = await _sut.ProcessAsync(
            orderResult, "zarinpal", "127.0.0.1", "agent/1.0", Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(orderResult);
        await _paymentService.DidNotReceiveWithAnyArgs().InitiatePaymentAsync(default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task ProcessAsync_WhenPaymentInitiated_EnrichesResultWithPaymentData()
    {
        var orderResult = NewOrderResult(150_000m);
        var userId = Guid.NewGuid();
        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<Money>(),
                Arg.Any<IpAddress>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("AUTH-123", "https://pay.example/AUTH-123", Guid.NewGuid()));

        var result = await _sut.ProcessAsync(
            orderResult, "zarinpal", "127.0.0.1", "agent/1.0", userId, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.OrderId.ShouldBe(orderResult.OrderId);
        result.Value.PaymentUrl.ShouldBe("https://pay.example/AUTH-123");
        result.Value.PaymentAuthority.ShouldBe("AUTH-123");
    }

    [Fact]
    public async Task ProcessAsync_ForwardsOrderUserAndAmountToPaymentService()
    {
        var orderResult = NewOrderResult(150_000m);
        var userId = Guid.NewGuid();
        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<Money>(),
                Arg.Any<IpAddress>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("AUTH-1", "https://pay.example/1", Guid.NewGuid()));

        await _sut.ProcessAsync(orderResult, null, "10.0.0.5", null, userId, CancellationToken.None);

        await _paymentService.Received(1).InitiatePaymentAsync(
            Arg.Is<global::Domain.Order.ValueObjects.OrderId>(id => id.Value == orderResult.OrderId),
            Arg.Is<Money>(m => m.Amount == 150_000m),
            Arg.Is<IpAddress>(ip => ip.Value == "10.0.0.5"),
            Arg.Is<global::Domain.User.ValueObjects.UserId>(u => u.Value == userId),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenOrderNotFound_ReturnsNotFound()
    {
        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<Money>(),
                Arg.Any<IpAddress>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(new OrderNotFoundException(global::Domain.Order.ValueObjects.OrderId.NewId())));

        var result = await _sut.ProcessAsync(
            NewOrderResult(150_000m), null, "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task ProcessAsync_WhenOrderAlreadyPaid_ReturnsConflict()
    {
        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<Money>(),
                Arg.Any<IpAddress>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(new OrderAlreadyPaidException(global::Domain.Order.ValueObjects.OrderId.NewId())));

        var result = await _sut.ProcessAsync(
            NewOrderResult(150_000m), null, "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public async Task ProcessAsync_WhenGatewayFails_ReturnsFailure()
    {
        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<global::Domain.Order.ValueObjects.OrderId>(),
                Arg.Any<Money>(),
                Arg.Any<IpAddress>(),
                Arg.Any<global::Domain.User.ValueObjects.UserId>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PaymentInitiationResult>(new ExternalServiceException("Zarinpal", "gateway down")));

        var result = await _sut.ProcessAsync(
            NewOrderResult(150_000m), null, "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }
}
