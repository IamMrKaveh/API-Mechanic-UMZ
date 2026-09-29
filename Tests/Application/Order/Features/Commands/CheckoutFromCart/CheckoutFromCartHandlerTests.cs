using Application.Common.Interfaces;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Application.Order.Features.Shared;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandlerTests : HandlerTestBase
{
    private readonly ICheckoutOrchestrationService _orchestration = Substitute.For<ICheckoutOrchestrationService>(); private readonly CheckoutFromCartHandler _sut;

    public CheckoutFromCartHandlerTests()
    {
        _sut = new CheckoutFromCartHandler(_orchestration, CurrentUserService);
    }

    private static CheckoutFromCartCommand NewCommand() =>
        new(
            CartId: Guid.NewGuid(),
            ShippingId: Guid.NewGuid(),
            AddressId: Guid.NewGuid(),
            DiscountCode: null,
            PaymentMethod: null,
            PaymentMethodId: null,
            IdempotencyKey: Guid.NewGuid());

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        CurrentUserService.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(NewCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orchestration.DidNotReceiveWithAnyArgs().ProcessCheckoutAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_EnrichesCommandWithUserContextAndDelegatesToOrchestration()
    {
        var userId = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)userId);
        CurrentUserService.IpAddress.Returns("127.0.0.1");
        CurrentUserService.UserAgent.Returns("agent/1.0");

        var expected = ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto { OrderId = Guid.NewGuid() });
        CheckoutFromCartCommand? captured = null;
        _orchestration
            .ProcessCheckoutAsync(Arg.Do<CheckoutFromCartCommand>(c => captured = c), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(NewCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured.IpAddress.ShouldBe("127.0.0.1");
        captured.UserAgent.ShouldBe("agent/1.0");
        await _orchestration.Received(1).ProcessCheckoutAsync(Arg.Any<CheckoutFromCartCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOrchestrationFails_PropagatesFailure()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IpAddress.Returns(string.Empty);
        CurrentUserService.UserAgent.Returns((string?)null);

        _orchestration
            .ProcessCheckoutAsync(Arg.Any<CheckoutFromCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutResultDto>.Validation("bad cart"));

        var result = await _sut.Handle(NewCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }
}
