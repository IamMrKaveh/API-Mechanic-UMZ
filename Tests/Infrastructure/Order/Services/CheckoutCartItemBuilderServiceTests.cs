using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Cart.Aggregates;
using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Infrastructure.Order.Services;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services;

public class CheckoutCartItemBuilderServiceTests
{
    private readonly ICartRepository _cartRepository = Substitute.For<ICartRepository>();
    private readonly CheckoutCartItemBuilderService _sut;

    public CheckoutCartItemBuilderServiceTests()
    {
        _sut = new CheckoutCartItemBuilderService(_cartRepository);
    }

    private static global::Domain.Cart.Aggregates.Cart NewUserCart(Guid userId, int quantity = 2, decimal unitPrice = 100_000m)
    {
        var cart = global::Domain.Cart.Aggregates.Cart.CreateForUser(global::Domain.User.ValueObjects.UserId.From(userId));
        new CartItemParametersBuilder()
            .WithQuantity(quantity)
            .WithUnitPrice(unitPrice, "IRT")
            .WithOriginalPrice(unitPrice, "IRT")
            .AddTo(cart);
        cart.ClearDomainEvents();
        return cart;
    }

    [Fact]
    public async Task BuildAsync_WhenCartDoesNotExist_ReturnsNotFound()
    {
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Cart.Aggregates.Cart?)null);

        var result = await _sut.BuildAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task BuildAsync_WhenCartBelongsToAnotherUser_ReturnsForbidden()
    {
        var cart = NewUserCart(Guid.NewGuid());
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.BuildAsync(cart.Id.Value, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task BuildAsync_WhenCartIsEmpty_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        var cart = global::Domain.Cart.Aggregates.Cart.CreateForUser(global::Domain.User.ValueObjects.UserId.From(userId));
        cart.ClearDomainEvents();
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.BuildAsync(cart.Id.Value, userId, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task BuildAsync_WhenCartHasItems_ReturnsSnapshotsAndSubtotal()
    {
        var userId = Guid.NewGuid();
        var cart = NewUserCart(userId, quantity: 2, unitPrice: 100_000m);
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.BuildAsync(cart.Id.Value, userId, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(1);
        result.Value.Items[0].Quantity.ShouldBe(2);
        result.Value.Items[0].UnitPrice.Amount.ShouldBe(100_000m);
        result.Value.SubTotal.ShouldBe(200_000m);
    }

    [Fact]
    public async Task BuildAsync_WhenCartHasMultipleItems_MapsAllItems()
    {
        var userId = Guid.NewGuid();
        var cart = global::Domain.Cart.Aggregates.Cart.CreateForUser(global::Domain.User.ValueObjects.UserId.From(userId));
        new CartItemParametersBuilder().WithQuantity(1).WithUnitPrice(50_000m, "IRT").WithOriginalPrice(50_000m, "IRT").AddTo(cart);
        new CartItemParametersBuilder().WithQuantity(3).WithUnitPrice(20_000m, "IRT").WithOriginalPrice(20_000m, "IRT").AddTo(cart);
        cart.ClearDomainEvents();
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        var result = await _sut.BuildAsync(cart.Id.Value, userId, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.Items.Count.ShouldBe(2);
        result.Value.SubTotal.ShouldBe(110_000m);
    }

    [Fact]
    public async Task BuildAsync_ForwardsCartIdAndCancellationToken()
    {
        var userId = Guid.NewGuid();
        var cart = NewUserCart(userId);
        var ct = new CancellationTokenSource().Token;
        _cartRepository
            .FindByIdAsync(Arg.Any<CartId>(), Arg.Any<CancellationToken>())
            .Returns(cart);

        await _sut.BuildAsync(cart.Id.Value, userId, ct);

        await _cartRepository.Received(1).FindByIdAsync(
            Arg.Is<CartId>(id => id == cart.Id), ct);
    }
}
