using Application.Audit.Contracts;
using Application.Payment.Contracts;
using Application.Payment.Features.Commands.VerifyPayment;
using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Domain.Payment.Exceptions;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Commands.VerifyPayment;

public class VerifyPaymentHandlerTests
{
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>(); private readonly IAuditService _auditService = Substitute.For<IAuditService>(); private readonly VerifyPaymentHandler _sut;

    public VerifyPaymentHandlerTests()
    {
        _sut = new VerifyPaymentHandler(_paymentService, _auditService);
    }

    [Theory]
    [InlineData("NOK")]
    [InlineData("cancel")]
    [InlineData("failed")]
    public async Task Handle_WhenStatusIsNotOk_LogsWarningAndReturnsFailure(string status)
    {
        var result = await _sut.Handle(new VerifyPaymentCommand("A123", status), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        await _auditService.Received(1).LogWarningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _paymentService.DidNotReceiveWithAnyArgs().VerifyPaymentAsync(default!, default);
    }

    [Theory]
    [InlineData("OK")]
    [InlineData("ok")]
    [InlineData("Ok")]
    public async Task Handle_WhenStatusIsOkVariants_ProceedsToVerify(string status)
    {
        var verification = new PaymentVerificationResult(Guid.NewGuid(), true, 12345, "6037", 0m);
        _paymentService
            .VerifyPaymentAsync("A123", Arg.Any<CancellationToken>())
            .Returns(verification);

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", status), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(verification);
        await _paymentService.Received(1).VerifyPaymentAsync("A123", Arg.Any<CancellationToken>());
        await _auditService.DidNotReceive().LogWarningAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentTransactionNotFoundExceptionThrown_ReturnsNotFound()
    {
        _paymentService
            .VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new PaymentTransactionNotFoundException("A123"));

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFoundExceptionThrown_ReturnsNotFound()
    {
        _paymentService
            .VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new OrderNotFoundException());

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPaymentNotVerifiableExceptionThrown_ReturnsFailure()
    {
        _paymentService
            .VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new PaymentNotVerifiableException(PaymentAuthority.Create("A12345")));

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_WhenExternalServiceExceptionThrown_ReturnsFailure()
    {
        _paymentService
            .VerifyPaymentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new ExternalServiceException("Zarinpal", "gateway down"));

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_WhenVerificationSucceedsWithTransactionId_LogsPaymentEvent()
    {
        var transactionId = Guid.NewGuid();
        _paymentService
            .VerifyPaymentAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(transactionId, true, 42L, null, 0m));

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        await _auditService.Received(1).LogPaymentEventAsync(
            Arg.Is<PaymentTransactionId>(x => x == PaymentTransactionId.From(transactionId)),
            "VerifyPayment",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenVerificationSucceedsWithoutTransactionId_DoesNotLogPaymentEvent()
    {
        _paymentService
            .VerifyPaymentAsync("A123", Arg.Any<CancellationToken>())
            .Returns(new PaymentVerificationResult(null, false, null, null, 0m));

        var result = await _sut.Handle(new VerifyPaymentCommand("A123", "OK"), CancellationToken.None);

        result.ShouldBeSuccess();
        await _auditService.DidNotReceiveWithAnyArgs().LogPaymentEventAsync(
            default!, default!, default!, default, default, default);
    }
}
