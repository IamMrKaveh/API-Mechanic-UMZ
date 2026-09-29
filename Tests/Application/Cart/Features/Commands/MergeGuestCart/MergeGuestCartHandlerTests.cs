using Application.Audit.Contracts;
using Application.Cart.Features.Commands.MergeGuestCart;
using Application.Common.Interfaces;
using Domain.Cart.Enum;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;
using SharedKernel.Abstractions.Interfaces;
using NSubstitute;

namespace Tests.Application.Cart.Features.Commands.MergeGuestCart;

public class MergeGuestCartHandlerTests : HandlerTestBase
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly MergeGuestCartHandler _sut;

    public MergeGuestCartHandlerTests()
    {
        _sut = new MergeGuestCartHandler(_cartRepository, AuditService, CurrentUserService, DateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenGuestTokenIsMissing_ReturnsSuccessAndSkipsMerge()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(new MergeGuestCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _cartRepository.DidNotReceiveWithAnyArgs().FindByGuestTokenAsync(default!, default);
        await _cartRepository.DidNotReceiveWithAnyArgs().FindByUserIdAsync(default!, default);
        await AuditService.DidNotReceiveWithAnyArgs().LogAsync(
            default!, default!, default!, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenGuestCartNotFound_ReturnsSuccessAndSkipsMerge()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.GuestToken.Returns("GUEST-TOKEN-MRG12345");
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new MergeGuestCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _cartRepository.DidNotReceiveWithAnyArgs().FindByUserIdAsync(default!, default);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Remove(Arg.Any<Carts>());
        await AuditService.DidNotReceiveWithAnyArgs().LogAsync(
            default!, default!, default!, default, default, default, default, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_AssignsGuestCartToUserAndLogsAudit()
    {
        var userGuid = Guid.NewGuid();
        var guestTokenValue = "GUEST-TOKEN-MRG22222";
        var guestCart = new CartBuilder()
            .ForGuest(GuestToken.Create(guestTokenValue))
            .Build();
        new CartItemParametersBuilder().WithQuantity(2).AddTo(guestCart);

        CurrentUserService.UserId.Returns((Guid?)userGuid);
        CurrentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(guestCart);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(new MergeGuestCartCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        guestCart.UserId.ShouldNotBeNull();
        guestCart.UserId!.Value.ShouldBe(userGuid);
        guestCart.GuestToken.ShouldBeNull();
        _cartRepository.Received(1).Update(guestCart);
        _cartRepository.DidNotReceive().Remove(Arg.Any<Carts>());
        await AuditService.Received(1).LogAsync(
            "Cart",
            "MergeGuestCart",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            "Cart",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenUserAndGuestBothHaveCarts_MergesGuestIntoUserAndRemovesGuest()
    {
        var userGuid = Guid.NewGuid();
        var guestTokenValue = "GUEST-TOKEN-MRG33333";
        var userCart = new CartBuilder().ForUser(UserId.From(userGuid)).Build();
        var guestCart = new CartBuilder()
            .ForGuest(GuestToken.Create(guestTokenValue))
            .Build();
        var sharedVariantId = VariantId.NewId();
        new CartItemParametersBuilder().WithVariantId(sharedVariantId).WithQuantity(2).AddTo(guestCart);

        CurrentUserService.UserId.Returns((Guid?)userGuid);
        CurrentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(guestCart);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(userCart);

        var result = await _sut.Handle(
            new MergeGuestCartCommand(CartMergeStrategy.SumQuantities),
            CancellationToken.None);

        result.ShouldBeSuccess();
        userCart.CartItems.Count.ShouldBe(1);
        userCart.CartItems.Single().VariantId.ShouldBe(sharedVariantId);
        userCart.CartItems.Single().Quantity.ShouldBe(2);
        _cartRepository.Received(1).Update(userCart);
        _cartRepository.Received(1).Remove(guestCart);
        await AuditService.Received(1).LogAsync(
            "Cart",
            "MergeGuestCart",
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            "Cart",
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }
}

