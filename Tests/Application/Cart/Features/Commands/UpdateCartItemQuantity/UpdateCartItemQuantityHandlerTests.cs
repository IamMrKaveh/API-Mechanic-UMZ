using Application.Cart.Contracts;
using Application.Cart.Features.Commands.UpdateCartItemQuantity;
using Application.Cart.Features.Shared;
using Application.Common.Interfaces;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Carts = Domain.Cart.Aggregates.Cart;
using Inventories = Domain.Inventory.Aggregates.Inventory;

namespace Tests.Application.Cart.Features.Commands.UpdateCartItemQuantity;

public class UpdateCartItemQuantityHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly ICartQueryService _cartQueryService = Substitute.For<ICartQueryService>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UpdateCartItemQuantityHandler _sut;

    public UpdateCartItemQuantityHandlerTests()
    {
        _sut = new UpdateCartItemQuantityHandler(
            _cartRepository,
            _variantRepository,
            _inventoryRepository,
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
            new UpdateCartItemQuantityCommand(Guid.NewGuid(), 3),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(Guid.NewGuid(), 3),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inventories?)null);

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(variantId.Value, 3),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenInventoryCannotFulfill_ReturnsValidationFailure()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 1, isUnlimited: false);

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(variantId.Value, 5),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoCart_ReturnsNotFound()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);

        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(variantId.Value, 3),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserCartHasItem_UpdatesQuantityAndPersistsAndReturnsDetail()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(2).AddTo(cart);
        var expectedDto = new CartDetailDto { Id = cart.Id.Value, TotalItems = 7 };

        _currentUserService.UserId.Returns((Guid?)userId.Value);
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(variantId.Value, 7),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expectedDto);
        cart.CartItems.Single().Quantity.ShouldBe(7);
        _cartRepository.Received(1).Update(cart);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenGuestCartHasItem_UpdatesQuantityAndPersistsAndReturnsDetail()
    {
        var guestTokenValue = "GUEST-TOKEN-UPD12345";
        var guestToken = GuestToken.Create(guestTokenValue);
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder().WithVariantId(variantId).WithQuantity(1).AddTo(cart);

        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestTokenValue);
        _variantRepository
            .GetByIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(cart);
        _cartQueryService
            .GetCartDetailAsync(Arg.Any<UserId?>(), Arg.Any<GuestToken?>(), Arg.Any<CancellationToken>())
            .Returns(new CartDetailDto { Id = cart.Id.Value });

        var result = await _sut.Handle(
            new UpdateCartItemQuantityCommand(variantId.Value, 4),
            CancellationToken.None);

        result.ShouldBeSuccess();
        cart.CartItems.Single().Quantity.ShouldBe(4);
        _cartRepository.Received(1).Update(cart);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
