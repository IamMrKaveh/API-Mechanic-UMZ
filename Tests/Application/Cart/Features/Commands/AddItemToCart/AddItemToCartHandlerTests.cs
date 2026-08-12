using Application.Cart.Features.Commands.AddItemToCart;
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

namespace Tests.Application.Cart.Features.Commands.AddItemToCart;

public class AddItemToCartHandlerTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>(); private readonly IVariantRepository _variantRepository = Substitute.For<IVariantRepository>(); private readonly IInventoryRepository _inventoryRepository = Substitute.For<IInventoryRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly AddItemToCartHandler _sut;

    public AddItemToCartHandlerTests()
    {
        _sut = new AddItemToCartHandler(
            _cartRepository,
            _variantRepository,
            _inventoryRepository,
            _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((ProductVariant?)null);

        var result = await _sut.Handle(
            new AddItemToCartCommand(Guid.NewGuid(), 2),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsFailure()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((Inventories?)null);

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 2),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenInventoryCannotFulfill_ReturnsFailure()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 1, isUnlimited: false);

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 5),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenNoUserAndNoGuestToken_ReturnsFailure()
    {
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns((string?)null);

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 2),
            CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoExistingCart_CreatesNewCartAndAddsItem()
    {
        var userGuid = Guid.NewGuid();
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _currentUserService.UserId.Returns((Guid?)userGuid);
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        Carts? added = null;
        _cartRepository
            .When(x => x.Add(Arg.Any<Carts>()))
            .Do(ci => added = ci.Arg<Carts>());

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 2),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.Received(1).Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        added.ShouldNotBeNull();
        added!.CartItems.Count.ShouldBe(1);
        added.CartItems.Single().VariantId.ShouldBe(variantId);
        added.CartItems.Single().Quantity.ShouldBe(2);
        added.UserId.ShouldNotBeNull();
        added.UserId!.Value.ShouldBe(userGuid);
    }

    [Fact]
    public async Task Handle_WhenUserHasExistingCart_AddsItemAndCallsUpdate()
    {
        var userGuid = Guid.NewGuid();
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);
        var existingCart = Carts.CreateForUser(UserId.From(userGuid));

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _currentUserService.UserId.Returns((Guid?)userGuid);
        _currentUserService.GuestToken.Returns((string?)null);
        _cartRepository
            .FindByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(existingCart);

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 3),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.Received(1).Update(existingCart);
        existingCart.CartItems.Count.ShouldBe(1);
        existingCart.CartItems.Single().VariantId.ShouldBe(variantId);
        existingCart.CartItems.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_WhenGuestHasNoExistingCart_CreatesNewCartAndAddsItem()
    {
        var guestTokenValue = "GUEST-TOKEN-ADD12345";
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 100);

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns((Carts?)null);

        Carts? added = null;
        _cartRepository
            .When(x => x.Add(Arg.Any<Carts>()))
            .Do(ci => added = ci.Arg<Carts>());

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 1),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.Received(1).Add(Arg.Any<Carts>());
        _cartRepository.DidNotReceive().Update(Arg.Any<Carts>());
        added.ShouldNotBeNull();
        added!.UserId.ShouldBeNull();
        added.GuestToken.ShouldNotBeNull();
        added.GuestToken!.Value.ShouldBe(guestTokenValue);
        added.CartItems.Single().VariantId.ShouldBe(variantId);
        added.CartItems.Single().Quantity.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenGuestHasExistingCart_AddsItemAndCallsUpdate()
    {
        var guestTokenValue = "GUEST-TOKEN-ADD98765";
        var variantId = VariantId.NewId();
        var variant = new ProductVariantBuilder().WithId(variantId).Build();
        var inventory = Inventories.Create(variantId, initialStock: 50);
        var existingCart = Carts.CreateForGuest(GuestToken.Create(guestTokenValue));

        _variantRepository
            .GetWithProductAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(variant);
        _inventoryRepository
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);
        _currentUserService.UserId.Returns((Guid?)null);
        _currentUserService.GuestToken.Returns(guestTokenValue);
        _cartRepository
            .FindByGuestTokenAsync(Arg.Any<GuestToken>(), Arg.Any<CancellationToken>())
            .Returns(existingCart);

        var result = await _sut.Handle(
            new AddItemToCartCommand(variantId.Value, 2),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _cartRepository.DidNotReceive().Add(Arg.Any<Carts>());
        _cartRepository.Received(1).Update(existingCart);
        existingCart.CartItems.Single().VariantId.ShouldBe(variantId);
        existingCart.CartItems.Single().Quantity.ShouldBe(2);
    }
}
