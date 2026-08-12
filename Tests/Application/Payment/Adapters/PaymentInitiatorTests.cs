using Application.Payment.Adapters;
using Application.Payment.Features.Commands.AtomicRefundPayment;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Payment.Adapters;

public class PaymentInitiatorTests
{
    private readonly ISender _mediator = Substitute.For<ISender>(); private readonly PaymentInitiator _sut;

    public PaymentInitiatorTests()
    {
        _sut = new PaymentInitiator(_mediator);
    }

    [Fact]
    public async Task InitiateRefundAsync_ForwardsAtomicRefundPaymentCommandToMediator()
    {
        var orderId = Guid.NewGuid();
        const string reason = "customer request";

        _mediator
            .Send(Arg.Any<AtomicRefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.InitiateRefundAsync(orderId, reason, CancellationToken.None);

        result.ShouldBeSuccess();
        await _mediator.Received(1).Send(
            Arg.Is<AtomicRefundPaymentCommand>(c => c.OrderId == orderId && c.Reason == reason),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InitiateRefundAsync_WhenMediatorReturnsFailure_PropagatesFailure()
    {
        _mediator
            .Send(Arg.Any<AtomicRefundPaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound("سفارش یافت نشد."));

        var result = await _sut.InitiateRefundAsync(Guid.NewGuid(), "reason", CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }
}
