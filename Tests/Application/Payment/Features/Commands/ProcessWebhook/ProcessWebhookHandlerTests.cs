using Application.Payment.Contracts;
using Application.Payment.Features.Commands.ProcessWebhook;
using Domain.Payment.Exceptions;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Features.Commands.ProcessWebhook;

public class ProcessWebhookHandlerTests
{
    private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>(); private readonly ProcessWebhookHandler _sut;

    public ProcessWebhookHandlerTests()
    {
        _sut = new ProcessWebhookHandler(_paymentService);
    }

    [Fact]
    public async Task Handle_WhenServiceCompletesNormally_ReturnsSuccess()
    {
        var cmd = new ProcessWebhookCommand("A123", "OK", "nonce-1");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.ShouldBeSuccess();
        await _paymentService.Received(1).ProcessWebhookAsync("A123", "OK", "nonce-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentTransactionNotFoundExceptionThrown_ReturnsNotFound()
    {
        _paymentService
            .ProcessWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new PaymentTransactionNotFoundException("A123"));

        var result = await _sut.Handle(new ProcessWebhookCommand("A123", "OK", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenExternalServiceExceptionThrown_ReturnsFailure()
    {
        _paymentService
            .ProcessWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new ExternalServiceException("Zarinpal", "boom"));

        var result = await _sut.Handle(new ProcessWebhookCommand("A123", "OK", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_WhenDomainExceptionThrown_ReturnsFailure()
    {
        _paymentService
            .ProcessWebhookAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new DomainException("invalid webhook"));

        var result = await _sut.Handle(new ProcessWebhookCommand("A123", "OK", null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
    }
}
