using Application.Cart.Features.Commands.ClearCart;
using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Application.Cart.Features.Commands.ClearCart;

public class ClearCartHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly ClearCartHandler _sut;

    public ClearCartHandlerTests()
    {
        _sut = new ClearCartHandler(_cartRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndInvalidGuestToken_ReturnsValidationFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new ClearCartCommand(), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Validation);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_ReturnsSuccessAndDoesNotUpdate()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new ClearCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserHasCart_ClearsCartAndCallsUpdate()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithQuantity(2).AddTo(cart);
        new CartItemParametersBuilder().WithQuantity(3).AddTo(cart);
        cart.CartItems.Count.ShouldBe(2);

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.Handle(new ClearCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.ShouldBeEmpty();
        _cartRepository.Received(1).Update(cart);
    }

    [Fact]
    public async Task Handle_WhenGuestHasNoCart_ReturnsSuccessAndDoesNotUpdate()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns("GUEST-TOKEN-CLR12345");
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new ClearCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenGuestHasCart_ClearsCartAndCallsUpdate()
    {
        var guestToken = GuestToken.Create("GUEST-TOKEN-CLR98765");
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder().AddTo(cart);
        cart.CartItems.Count.ShouldBe(1);

        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestToken.Value);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.Handle(new ClearCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.ShouldBeEmpty();
        _cartRepository.Received(1).Update(cart);
    }
}
