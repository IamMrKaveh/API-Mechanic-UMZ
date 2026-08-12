using Application.Cart.Contracts;
using Application.Cart.Features.Commands.RemoveItemFromCart;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Application.Cart.Features.Commands.RemoveItemFromCart;

public class RemoveItemFromCartHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly RemoveItemFromCartHandler _sut;

    public RemoveItemFromCartHandlerTests()
    {
        _sut = new RemoveItemFromCartHandler(
            _cartRepository,
            _cartQueryService,
            _unitOfWork,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsValidationFailure()
    {
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(
            new RemoveItemFromCartCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(
            new RemoveItemFromCartCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenUserCartHasItem_RemovesItemAndPersistsAndReturnsDetail()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(2).AddTo(cart);
        var expectedDto = new CartDetailDto { Id = cart.Id.Value, TotalItems = 0 };

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var result = await _sut.Handle(
            new RemoveItemFromCartCommand(variantId.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
        cart.CartItems.ShouldBeEmpty();
        _cartRepository.Received(1).Update(cart);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGuestCartHasItem_RemovesItemAndPersistsAndReturnsDetail()
    {
        var guestTokenValue = "GUEST-TOKEN-RMV12345";
        var guestToken = GuestToken.Create(guestTokenValue);
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(4).AddTo(cart);
        var expectedDto = new CartDetailDto { Id = cart.Id.Value };

        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var result = await _sut.Handle(
            new RemoveItemFromCartCommand(variantId.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
        cart.CartItems.ShouldBeEmpty();
        _cartRepository.Received(1).Update(cart);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQueryServiceReturnsNull_ReturnsSuccessWithEmptyDto()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithVariantId(variantId).AddTo(cart);

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns((CartDetailDto?)null);

        var result = await _sut.Handle(
            new RemoveItemFromCartCommand(variantId.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Items.ShouldBeEmpty();
    }
}
