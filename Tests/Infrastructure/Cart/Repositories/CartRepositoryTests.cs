using Domain.Cart.Interfaces;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Cart.Repositories;
using Infrastructure.Persistence.Context;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Cart.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private ICartRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new CartRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> PersistUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return user;
    }

    [Fact]
    public async Task Add_ThenFindByIdAsync_ReturnsCartAcrossContexts()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new CartRepository(freshContext);
        var loaded = await freshRepo.FindByIdAsync(cart.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(cart.Id);
        loaded.UserId.ShouldBe(user.Id);
        loaded.IsCheckedOut.ShouldBeFalse();
    }

    [Fact]
    public async Task FindByIdAsync_WhenCartDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.FindByIdAsync(CartId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenActiveCartExists_ReturnsCart()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByUserIdAsync(user.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(cart.Id);
        loaded.UserId.ShouldBe(user.Id);
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenCartIsCheckedOut_ReturnsNull()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();

        new CartItemParametersBuilder()
            .WithQuantity(1)
            .AddTo(cart);

        cart.Checkout();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByUserIdAsync(user.Id);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenUserHasNoCart_ReturnsNull()
    {
        var loaded = await _sut.FindByUserIdAsync(UserId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByGuestTokenAsync_WhenActiveGuestCartExists_ReturnsCart()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByGuestTokenAsync(guestToken);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(cart.Id);
        loaded.GuestToken.ShouldNotBeNull();
        loaded.GuestToken!.Value.ShouldBe(guestToken.Value);
    }

    [Fact]
    public async Task FindByGuestTokenAsync_WhenCartIsCheckedOut_ReturnsNull()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();

        new CartItemParametersBuilder()
            .WithQuantity(1)
            .AddTo(cart);

        cart.Checkout();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByGuestTokenAsync(guestToken);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByIdAsync_WithCartItems_LoadsIncludedItems()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();

        new CartItemParametersBuilder()
            .WithProductName("Product X")
            .WithSku("SKU-CART-1")
            .WithUnitPrice(150m, "IRT")
            .WithOriginalPrice(200m, "IRT")
            .WithQuantity(2)
            .AddTo(cart);

        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByIdAsync(cart.Id);

        loaded.ShouldNotBeNull();
        loaded!.CartItems.Count.ShouldBe(1);
        var item = loaded.CartItems.Single();
        item.ProductName.Value.ShouldBe("Product X");
        item.Sku.Value.ShouldBe("SKU-CART-1");
        item.Quantity.ShouldBe(2);
        item.SellingPrice.Amount.ShouldBe(150m);
        item.OriginalPrice.Amount.ShouldBe(200m);
    }

    [Fact]
    public async Task Update_AfterAddingItem_PersistsChange()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByIdAsync(cart.Id);
        loaded.ShouldNotBeNull();

        new CartItemParametersBuilder()
            .WithQuantity(3)
            .AddTo(loaded!);
        loaded!.ClearDomainEvents();

        _sut.Update(loaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new CartRepository(freshContext);
        var final = await freshRepo.FindByIdAsync(cart.Id);

        final.ShouldNotBeNull();
        final!.CartItems.Count.ShouldBe(1);
        final.CartItems.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Remove_ExistingCart_DeletesFromDatabase()
    {
        var user = await PersistUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        _sut.Add(cart);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _sut.FindByIdAsync(cart.Id);
        toRemove.ShouldNotBeNull();
        _sut.Remove(toRemove!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByIdAsync(cart.Id);
        loaded.ShouldBeNull();
    }
}
