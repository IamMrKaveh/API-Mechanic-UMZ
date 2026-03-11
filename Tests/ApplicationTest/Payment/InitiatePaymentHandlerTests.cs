using Application.Common.Models;
using Domain.Common.ValueObjects;

namespace Tests.ApplicationTest.Payment;

public class InitiatePaymentHandlerTests
{
    private readonly IPaymentService _paymentService;
    private readonly InitiatePaymentHandler _handler;

    public InitiatePaymentHandlerTests()
    {
        _paymentService = Substitute.For<IPaymentService>();
        _handler = new InitiatePaymentHandler(_paymentService);
    }

    private static InitiatePaymentCommand BuildCommand()
        => new InitiatePaymentCommand(
            UserId: 1,
            Dto: new PaymentInitiationDto
            {
                OrderId = 1,
                UserId = 1,
                Amount = Money.FromDecimal(500_000m),
                Description = "پرداخت سفارش",
                CallbackUrl = "https://example.com/callback"
            });

    [Fact]
    public async Task Handle_WhenPaymentServiceSucceeds_ShouldReturnSuccess()
    {
        var serviceResult = ServiceResult<(bool IsSuccess, string? Authority, string? PaymentUrl, string? Message)>
            .Success((true, "AUTH123", "https://payment.com/pay/AUTH123", null));
        _paymentService.InitiatePaymentAsync(Arg.Any<PaymentInitiationDto>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Authority.Should().Be("AUTH123");
    }

    [Fact]
    public async Task Handle_WhenPaymentServiceFails_ShouldReturnFailure()
    {
        var serviceResult = ServiceResult<(bool IsSuccess, string? Authority, string? PaymentUrl, string? Message)>
            .Failure("درگاه پرداخت در دسترس نیست.", 503);
        _paymentService.InitiatePaymentAsync(Arg.Any<PaymentInitiationDto>(), Arg.Any<CancellationToken>()).Returns(serviceResult);

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ShouldReturnFailure()
    {
        _paymentService.InitiatePaymentAsync(Arg.Any<PaymentInitiationDto>(), Arg.Any<CancellationToken>())
            .Throws(new DomainException("مبلغ نامعتبر"));

        var result = await _handler.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("مبلغ نامعتبر");
    }
}