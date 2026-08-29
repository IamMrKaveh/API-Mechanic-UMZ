using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Cart.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Cart.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartRepositoryIntegrationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private CartRepository _sut = null!;

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

    [Fact]
    public async Task Add_AndFindByIdAsync_PersistsCartAndReturnsItWithCartItems()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();

        var itemParams = new CartItemParametersBuilder()
            .WithProductName("Product Alpha")
            .WithSku("SKU-ALPHA-001")
            .WithUnitPrice(150m, "IRT")
            .WithOriginalPrice(180m, "IRT")
            .WithQuantity(2);
        itemParams.AddTo(cart);

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByIdAsync(cart.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(cart.Id);
        reloaded.UserId.ShouldBe(userId);
        reloaded.GuestToken.ShouldBeNull();
        reloaded.IsCheckedOut.ShouldBeFalse();
        reloaded.CartItems.Count.ShouldBe(1);

        var reloadedItem = reloaded.CartItems.Single();
        reloadedItem.VariantId.ShouldBe(itemParams.VariantId);
        reloadedItem.ProductId.ShouldBe(itemParams.ProductId);
        reloadedItem.ProductName.ShouldBe(itemParams.ProductName);
        reloadedItem.Sku.ShouldBe(itemParams.Sku);
        reloadedItem.Quantity.ShouldBe(2);
        reloadedItem.SellingPrice.Amount.ShouldBe(150m);
        reloadedItem.SellingPrice.Currency.ShouldBe("IRT");
        reloadedItem.OriginalPrice.Amount.ShouldBe(180m);
        reloadedItem.OriginalPrice.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task FindByIdAsync_WhenCartDoesNotExist_ReturnsNull()
    {
        var result = await _sut.FindByIdAsync(CartId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenActiveUserCartExists_ReturnsCartWithItems()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithQuantity(3).AddTo(cart);

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByUserIdAsync(userId);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(cart.Id);
        reloaded.CartItems.Count.ShouldBe(1);
        reloaded.CartItems.Single().Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenCartIsCheckedOut_ReturnsNull()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().AddTo(cart);
        cart.Checkout();

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByUserIdAsync(userId);

        reloaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByUserIdAsync_WhenNoCartForUser_ReturnsNull()
    {
        var result = await _sut.FindByUserIdAsync(UserId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task FindByGuestTokenAsync_WhenActiveGuestCartExists_ReturnsCartWithItems()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder().WithQuantity(4).AddTo(cart);

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByGuestTokenAsync(guestToken);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(cart.Id);
        reloaded.GuestToken.ShouldBe(guestToken);
        reloaded.UserId.ShouldBeNull();
        reloaded.CartItems.Count.ShouldBe(1);
        reloaded.CartItems.Single().Quantity.ShouldBe(4);
    }

    [Fact]
    public async Task FindByGuestTokenAsync_WhenGuestCartIsCheckedOut_ReturnsNull()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        new CartItemParametersBuilder().AddTo(cart);
        cart.Checkout();

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByGuestTokenAsync(guestToken);

        reloaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByGuestTokenAsync_WhenNoCartForToken_ReturnsNull()
    {
        var result = await _sut.FindByGuestTokenAsync(GuestToken.Generate());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task Update_AfterAddingAnotherItem_PersistsAdditionalCartItem()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().WithSku("SKU-1").AddTo(cart);

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using (var mutateContext = _fixture.CreateContext())
        {
            var mutateRepo = new CartRepository(mutateContext);
            var tracked = await mutateRepo.FindByIdAsync(cart.Id);
            tracked.ShouldNotBeNull();

            new CartItemParametersBuilder().WithSku("SKU-2").AddTo(tracked!);
            mutateRepo.Update(tracked!);
            await mutateContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);
        var reloaded = await readRepo.FindByIdAsync(cart.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.CartItems.Count.ShouldBe(2);
        reloaded.CartItems.Select(i => i.Sku.Value).ShouldContain("SKU-1");
        reloaded.CartItems.Select(i => i.Sku.Value).ShouldContain("SKU-2");
    }

    [Fact]
    public async Task Remove_DeletesCartAndCascadesToCartItems()
    {
        var userId = UserId.NewId();
        var cart = new CartBuilder().ForUser(userId).Build();
        new CartItemParametersBuilder().AddTo(cart);
        new CartItemParametersBuilder().WithSku("SKU-EXTRA").WithVariantId(VariantId.NewId()).AddTo(cart);

        _sut.Add(cart);
        await _context.SaveChangesAsync();

        await using (var removeContext = _fixture.CreateContext())
        {
            var removeRepo = new CartRepository(removeContext);
            var tracked = await removeRepo.FindByIdAsync(cart.Id);
            tracked.ShouldNotBeNull();

            removeRepo.Remove(tracked!);
            await removeContext.SaveChangesAsync();
        }

        await using var readContext = _fixture.CreateContext();
        var readRepo = new CartRepository(readContext);
        var reloaded = await readRepo.FindByIdAsync(cart.Id);

        reloaded.ShouldBeNull();

        var remainingItems = await readContext.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .CountAsync();
        remainingItems.ShouldBe(0);
    }
}
