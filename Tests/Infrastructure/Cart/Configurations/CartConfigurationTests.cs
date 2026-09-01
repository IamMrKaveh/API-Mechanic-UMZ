using Domain.Cart.ValueObjects;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Cart.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CartConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SaveChanges_PersistsUserCartAndRoundTripsAllScalarProperties()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Carts.FirstOrDefaultAsync(c => c.Id == cart.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(cart.Id);
        reloaded.UserId.ShouldBe(user.Id);
        reloaded.GuestToken.ShouldBeNull();
        reloaded.AppliedDiscountCodeId.ShouldBeNull();
        reloaded.IsCheckedOut.ShouldBeFalse();
        reloaded.CreatedAt.ShouldNotBe(default);

        var rowVersion = Context.Entry(reloaded).Property<byte[]>("RowVersion").CurrentValue;
        rowVersion.ShouldNotBeNull();
        rowVersion!.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task SaveChanges_PersistsGuestCartWithGuestTokenAndNullUserId()
    {
        var guestToken = GuestToken.Generate();
        var cart = new CartBuilder().ForGuest(guestToken).Build();
        cart.ClearDomainEvents();

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Carts.FirstAsync(c => c.Id == cart.Id);

        reloaded.UserId.ShouldBeNull();
        reloaded.GuestToken.ShouldNotBeNull();
        reloaded.GuestToken!.Value.ShouldBe(guestToken.Value);
    }

    [Fact]
    public async Task SaveChanges_WhenUserOfCartIsDeleted_SetsUserIdToNull()
    {
        var user = await SeedUserAsync();
        var cart = new CartBuilder().ForUser(user.Id).Build();
        cart.ClearDomainEvents();

        Context.Carts.Add(cart);
        await Context.SaveChangesAsync();

        Context.Users.Remove(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Carts.FirstAsync(c => c.Id == cart.Id);

        reloaded.UserId.ShouldBeNull();
    }

    [Fact]
    public void Model_MappedToCartsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("Carts");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Domain.Cart.Aggregates.Cart.Id));
    }

    [Fact]
    public void Model_GuestToken_HasMaxLength256AndIsNullable()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(Domain.Cart.Aggregates.Cart.GuestToken));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
        property.GetMaxLength().ShouldBe(256);
    }

    [Fact]
    public void Model_UserId_IsNullable()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(Domain.Cart.Aggregates.Cart.UserId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Model_AppliedDiscountCodeId_IsNullable()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var property = entityType!.FindProperty(nameof(Domain.Cart.Aggregates.Cart.AppliedDiscountCodeId));
        property.ShouldNotBeNull();
        property!.IsNullable.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasRowVersionShadowProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var rowVersion = entityType!.FindProperty("RowVersion");
        rowVersion.ShouldNotBeNull();
        rowVersion!.IsConcurrencyToken.ShouldBeTrue();
        rowVersion.ValueGenerated.ShouldBe(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate);
    }

    [Fact]
    public void Model_HasCompositeIndexOnUserIdAndIsCheckedOut()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(Domain.Cart.Aggregates.Cart.UserId))
                && i.Properties.Any(p => p.Name == nameof(Domain.Cart.Aggregates.Cart.IsCheckedOut)));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasCompositeIndexOnGuestTokenAndIsCheckedOut()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(Domain.Cart.Aggregates.Cart.GuestToken))
                && i.Properties.Any(p => p.Name == nameof(Domain.Cart.Aggregates.Cart.IsCheckedOut)));

        index.ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasSetNullDeleteBehaviorOnUserId()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var foreignKey = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1
                && fk.Properties[0].Name == nameof(Domain.Cart.Aggregates.Cart.UserId));

        foreignKey.ShouldNotBeNull();
        foreignKey!.DeleteBehavior.ShouldBe(DeleteBehavior.SetNull);
        foreignKey.IsRequired.ShouldBeFalse();
    }

    [Fact]
    public void Model_HasCascadeDeleteFromCartToCartItems()
    {
        var entityType = Context.Model.FindEntityType(typeof(Domain.Cart.Aggregates.Cart));
        entityType.ShouldNotBeNull();

        var navigation = entityType!.FindNavigation(nameof(Domain.Cart.Aggregates.Cart.CartItems));
        navigation.ShouldNotBeNull();

        navigation!.ForeignKey.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
    }
}
