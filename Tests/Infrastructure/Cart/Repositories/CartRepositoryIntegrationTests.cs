using Domain.Cart.ValueObjects;
using Infrastructure.Cart.Repositories;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Cart.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartRepositoryIntegrationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private CartRepository _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sut = new CartRepository(Context);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Add_AndFindByIdAsync_PersistsCartAndReturnsItWithCartItems()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();

        var itemParams = new CartItemParametersBuilder()
            .WithProductName("Product Alpha")
            .WithSku("SKU-ALPHA-001")
            .WithUnitPrice(150m, "IRT")
            .WithOriginalPrice(180m, "IRT")
            .WithQuantity(2);
        itemParams.AddTo(cart);

        _sut.Add(cart);
        await Context.SaveChangesAsync();

        await using var readContext = Fixture.CreateContext();
        var readRepo = new CartRepository(readContext);

        var reloaded = await readRepo.FindByIdAsync(cart.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(cart.Id);
        reloaded.UserId.ShouldBe(user.Id);
        reloaded.GuestToken.ShouldBeNull();
        reloaded.IsCheckedOut.ShouldBeFalse();
        reloaded.CartItems.Count.ShouldBe(1);

        var reloadedItem = reloaded.CartItems.Single();
        reloadedItem.VariantId.ShouldBe(itemParams.VariantId);
        reloadedItem.ProductId.ShouldBe(itemParams.ProductId);
        reloadedItem.ProductName.Value.ShouldBe("Product Alpha");
        reloadedItem.VariantSku.Value.ShouldBe("SKU-ALPHA-001");
        reloadedItem.SellingPrice.Amount.ShouldBe(150m);
        reloadedItem.Quantity.ShouldBe(2);
    }

    [Fact]
    public async Task FindActiveByUserIdAsync_WhenActiveCartExists_ReturnsCart()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        new CartItemParametersBuilder().AddTo(cart);

        _sut.Add(cart);
        await Context.SaveChangesAsync();

        var found = await _sut.FindByUserIdAsync(user.Id);

        found.ShouldNotBeNull();
        found!.Id.ShouldBe(cart.Id);
        found.IsCheckedOut.ShouldBeFalse();
    }

    [Fact]
    public async Task FindActiveByUserIdAsync_WhenCartIsCheckedOut_ReturnsNull()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        new CartItemParametersBuilder().AddTo(cart);
        cart.Checkout();

        _sut.Add(cart);
        await Context.SaveChangesAsync();

        var found = await _sut.FindByUserIdAsync(user.Id);

        found.ShouldBeNull();
    }

    [Fact]
    public async Task FindActiveByGuestTokenAsync_WhenActiveCartExists_ReturnsCart()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();

        _sut.Add(cart);
        await Context.SaveChangesAsync();

        var found = await _sut.FindByGuestTokenAsync(guestToken);

        found.ShouldNotBeNull();
        found!.GuestToken.ShouldBe(guestToken);
    }

    [Fact]
    public async Task Update_WhenItemAddedToExistingCart_PersistsNewItem()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        _sut.Add(cart);
        await Context.SaveChangesAsync();

        var newItem = new CartItemParametersBuilder().WithSku("NEW-SKU");
        newItem.AddTo(cart);

        _sut.Update(cart);
        await Context.SaveChangesAsync();

        await using var readContext = Fixture.CreateContext();
        var reloaded = await new CartRepository(readContext).FindByIdAsync(cart.Id);

        reloaded!.CartItems.Any(i => i.VariantSku.Value == "NEW-SKU").ShouldBeTrue();
    }

    [Fact]
    public async Task Remove_DeletesCartFromDatabase()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        _sut.Add(cart);
        await Context.SaveChangesAsync();

        _sut.Remove(cart);
        await Context.SaveChangesAsync();

        var found = await _sut.FindByIdAsync(cart.Id);
        found.ShouldBeNull();
    }
}
