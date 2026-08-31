using Application.Cart.Contracts;
using Application.Media.Contracts;
using Application.Media.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Cart.QueryServices;
using Carts = Domain.Cart.Aggregates.Cart;

namespace Tests.Infrastructure.Cart.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IMediaQueryService _mediaService = null!;
    private ICartQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _mediaService = Substitute.For<IMediaQueryService>();
        _mediaService.GetPrimaryByEntitiesAsync(
                Arg.Any<string>(),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, MediaDto>());

        _sut = new CartQueryService(_context, _mediaService);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static Carts CreateUserCartWithItems(
        UserId userId,
        params (VariantId variantId, ProductId productId, string productName, string sku, decimal unitPrice, decimal originalPrice, int quantity)[] items)
    {
        var cart = Carts.CreateForUser(userId);
        foreach (var item in items)
        {
            cart.AddItem(
                item.variantId,
                item.productId,
                ProductName.Create(item.productName),
                Sku.Create(item.sku),
                Money.Create(item.unitPrice, "IRT"),
                Money.Create(item.originalPrice, "IRT"),
                item.quantity);
        }
        return cart;
    }

    [Fact]
    public async Task GetCartDetailAsync_NoUserNoGuestToken_ReturnsNull()
    {
        var result = await _sut.GetCartDetailAsync(userId: null, guestToken: null);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCartDetailAsync_UserWithoutCart_ReturnsNull()
    {
        var result = await _sut.GetCartDetailAsync(userId: UserId.NewId(), guestToken: null);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCartDetailAsync_UserWithCart_ReturnsMappedCart()
    {
        var userId = UserId.NewId();
        var variantId = VariantId.NewId();
        var productId = ProductId.NewId();

        var cart = CreateUserCartWithItems(userId,
            (variantId, productId, "Test Product", "SKU-100", 100_000m, 120_000m, 2));

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCartDetailAsync(userId, guestToken: null);

        result.ShouldNotBeNull();
        result!.UserId.ShouldBe(userId.Value);
        result.GuestToken.ShouldBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].VariantId.ShouldBe(variantId.Value);
        result.Items[0].ProductId.ShouldBe(productId.Value);
        result.Items[0].ProductName.ShouldBe("Test Product");
        result.Items[0].VariantSku.ShouldBe("SKU-100");
        result.Items[0].Quantity.ShouldBe(2);
        result.Items[0].UnitPrice.ShouldBe(100_000m);
        result.Items[0].TotalPrice.ShouldBe(200_000m);
        result.TotalPrice.ShouldBe(200_000m);
        result.TotalItems.ShouldBe(2);
    }

    [Fact]
    public async Task GetCartDetailAsync_CartWithMultipleItems_SumsCorrectly()
    {
        var userId = UserId.NewId();
        var cart = CreateUserCartWithItems(userId,
            (VariantId.NewId(), ProductId.NewId(), "Product A", "SKU-A", 50_000m, 60_000m, 3),
            (VariantId.NewId(), ProductId.NewId(), "Product B", "SKU-B", 200_000m, 220_000m, 1));

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCartDetailAsync(userId, guestToken: null);

        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBe(2);
        result.TotalItems.ShouldBe(4);
        result.TotalPrice.ShouldBe(3 * 50_000m + 1 * 200_000m);
    }

    [Fact]
    public async Task GetCartDetailAsync_UserProvided_DoesNotReturnCheckedOutCart()
    {
        var userId = UserId.NewId();
        var cart = CreateUserCartWithItems(userId,
            (VariantId.NewId(), ProductId.NewId(), "Item", "SKU-X", 100m, 100m, 1));

        typeof(Carts)
            .GetProperty(nameof(Carts.IsCheckedOut))!
            .SetValue(cart, true);

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCartDetailAsync(userId, guestToken: null);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCartSummaryAsync_NoUserNoGuestToken_ReturnsEmptySummary()
    {
        var result = await _sut.GetCartSummaryAsync(userId: null, guestToken: null);

        result.ShouldNotBeNull();
        result.ItemCount.ShouldBe(0);
        result.TotalQuantity.ShouldBe(0);
        result.TotalPrice.ShouldBe(0m);
    }

    [Fact]
    public async Task GetCartSummaryAsync_UserWithoutCart_ReturnsEmptySummary()
    {
        var result = await _sut.GetCartSummaryAsync(userId: UserId.NewId(), guestToken: null);

        result.ShouldNotBeNull();
        result.ItemCount.ShouldBe(0);
        result.TotalQuantity.ShouldBe(0);
        result.TotalPrice.ShouldBe(0m);
    }

    [Fact]
    public async Task GetCartSummaryAsync_UserWithCart_ReturnsAggregatedSummary()
    {
        var userId = UserId.NewId();
        var cart = CreateUserCartWithItems(userId,
            (VariantId.NewId(), ProductId.NewId(), "A", "SKU-1", 100m, 100m, 2),
            (VariantId.NewId(), ProductId.NewId(), "B", "SKU-2", 300m, 300m, 5));

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCartSummaryAsync(userId, guestToken: null);

        result.ShouldNotBeNull();
        result.ItemCount.ShouldBe(2);
        result.TotalQuantity.ShouldBe(7);
        result.TotalPrice.ShouldBe(2 * 100m + 5 * 300m);
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_NoUserNoGuestToken_ReturnsCartNotFoundError()
    {
        var result = await _sut.ValidateCartForCheckoutAsync(userId: null, guestToken: null);

        result.ShouldNotBeNull();
        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("سبد");
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_UserWithoutCart_ReturnsCartNotFoundError()
    {
        var result = await _sut.ValidateCartForCheckoutAsync(userId: UserId.NewId(), guestToken: null);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_UserWithEmptyCart_ReturnsCartEmptyError()
    {
        var userId = UserId.NewId();
        var cart = Carts.CreateForUser(userId);
        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.ValidateCartForCheckoutAsync(userId, guestToken: null);

        result.IsValid.ShouldBeFalse();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].ShouldContain("خالی");
    }

    [Fact]
    public async Task ValidateCartForCheckoutAsync_UserWithNonEmptyCart_ReturnsValid()
    {
        var userId = UserId.NewId();
        var cart = CreateUserCartWithItems(userId,
            (VariantId.NewId(), ProductId.NewId(), "Product", "SKU-Z", 500m, 500m, 1));

        _context.Carts.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.ValidateCartForCheckoutAsync(userId, guestToken: null);

        result.IsValid.ShouldBeTrue();
        result.Errors.Count.ShouldBe(0);
    }
}
