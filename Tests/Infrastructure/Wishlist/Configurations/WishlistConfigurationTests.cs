using Tests.TestInfrastructure.Base;
using Wishlists = global::Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Infrastructure.Wishlist.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WishlistConfigurationTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<(global::Domain.User.Aggregates.User user, global::Domain.Product.Aggregates.Product product)> SeedUserAndProductAsync(
        CancellationToken ct = default)
    {
        var user = await SeedUserAsync(ct: ct);
        var (brand, category) = await SeedBrandWithCategoryAsync(ct);
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var product = new ProductBuilder()
            .WithName($"Wishlist Product {suffix}")
            .WithSlug($"wishlist-product-{suffix}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();

        Context.Products.Add(product);
        await Context.SaveChangesAsync(ct);
        return (user, product);
    }

    private async Task<Wishlists> PersistAsync(Wishlists wishlist, CancellationToken ct = default)
    {
        wishlist.ClearDomainEvents();
        Context.Wishlists.Add(wishlist);
        await Context.SaveChangesAsync(ct);
        return wishlist;
    }

    [Fact]
    public async Task SaveChanges_NewWishlistItem_RoundTripsAllProperties()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var wishlist = new WishlistBuilder()
            .WithUserId(user.Id)
            .WithProductId(product.Id)
            .Build();
        await PersistAsync(wishlist);
        Context.ChangeTracker.Clear();

        var reloaded = await Context.Wishlists.FirstOrDefaultAsync(w => w.Id == wishlist.Id);

        reloaded.ShouldNotBeNull();
        reloaded!.Id.ShouldBe(wishlist.Id);
        reloaded.UserId.ShouldBe(user.Id);
        reloaded.ProductId.ShouldBe(product.Id);
        reloaded.CreatedAt.ShouldNotBe(default);
        reloaded.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task SaveChanges_DuplicateUserProduct_ThrowsDbUpdateException()
    {
        var (user, product) = await SeedUserAndProductAsync();
        await PersistAsync(new WishlistBuilder().WithUserId(user.Id).WithProductId(product.Id).Build());

        var duplicate = new WishlistBuilder().WithUserId(user.Id).WithProductId(product.Id).Build();
        duplicate.ClearDomainEvents();

        Context.Wishlists.Add(duplicate);

        await Should.ThrowAsync<DbUpdateException>(() => Context.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_SameProductForDifferentUsers_BothPersist()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var otherUser = await SeedUserAsync();

        await PersistAsync(new WishlistBuilder().WithUserId(user.Id).WithProductId(product.Id).Build());
        await PersistAsync(new WishlistBuilder().WithUserId(otherUser.Id).WithProductId(product.Id).Build());

        var count = await Context.Wishlists.CountAsync(w => w.ProductId == product.Id);
        count.ShouldBe(2);
    }

    [Fact]
    public async Task SaveChanges_WhenUserIsDeleted_WishlistItemsAreCascadeDeleted()
    {
        var (user, product) = await SeedUserAndProductAsync();
        await PersistAsync(new WishlistBuilder().WithUserId(user.Id).WithProductId(product.Id).Build());
        Context.ChangeTracker.Clear();

        var loaded = await Context.Users
            .IgnoreQueryFilters()
            .FirstAsync(u => u.Id == user.Id);
        Context.Users.Remove(loaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var remaining = await Context.Wishlists
            .IgnoreQueryFilters()
            .CountAsync(w => w.UserId == user.Id);
        remaining.ShouldBe(0);
    }

    [Fact]
    public async Task QueryFilter_WhenUserIsDeactivated_WishlistIsHidden()
    {
        var (user, product) = await SeedUserAndProductAsync();
        var wishlist = await PersistAsync(new WishlistBuilder().WithUserId(user.Id).WithProductId(product.Id).Build());

        user.Deactivate();
        user.ClearDomainEvents();
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var visible = await Context.Wishlists.FirstOrDefaultAsync(w => w.Id == wishlist.Id);
        visible.ShouldBeNull();

        var hidden = await Context.Wishlists.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.Id == wishlist.Id);
        hidden.ShouldNotBeNull();
    }

    [Fact]
    public void Model_MappedToWishlistsTable()
    {
        Skip.IfNot(Fixture.IsDockerAvailable, Fixture.UnavailabilityReason ?? "Docker engine not available.");

        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();
        entityType!.GetTableName().ShouldBe("Wishlists");
    }

    [Fact]
    public void Model_PrimaryKey_IsIdProperty()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();

        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.ShouldNotBeNull();
        primaryKey!.Properties.Count.ShouldBe(1);
        primaryKey.Properties[0].Name.ShouldBe(nameof(Wishlists.Id));
    }

    [Fact]
    public void Model_UserIdAndProductId_AreRequiredWithConverters()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();

        var userId = entityType!.FindProperty(nameof(Wishlists.UserId));
        userId.ShouldNotBeNull();
        userId!.IsNullable.ShouldBeFalse();
        userId.GetValueConverter().ShouldNotBeNull();

        var productId = entityType.FindProperty(nameof(Wishlists.ProductId));
        productId.ShouldNotBeNull();
        productId!.IsNullable.ShouldBeFalse();
        productId.GetValueConverter().ShouldNotBeNull();
    }

    [Fact]
    public void Model_HasUniqueIndexOnUserIdProductId()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();

        var index = entityType!.GetIndexes()
            .SingleOrDefault(i => i.Properties.Count == 2
                && i.Properties.Any(p => p.Name == nameof(Wishlists.UserId))
                && i.Properties.Any(p => p.Name == nameof(Wishlists.ProductId)));
        index.ShouldNotBeNull();
        index!.IsUnique.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasSingleColumnIndexesOnUserIdAndProductId()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();

        foreach (var propertyName in new[] { nameof(Wishlists.UserId), nameof(Wishlists.ProductId) })
        {
            var index = entityType!.GetIndexes()
                .SingleOrDefault(i => i.Properties.Count == 1 && i.Properties[0].Name == propertyName);
            index.ShouldNotBeNull($"index on {propertyName} should exist");
        }
    }

    [Fact]
    public void Model_UserAndProduct_ForeignKeysHaveCascadeDelete()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();

        var userFk = entityType!.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Wishlists.UserId));
        userFk.ShouldNotBeNull();
        userFk!.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        userFk.IsRequired.ShouldBeTrue();

        var productFk = entityType.GetForeignKeys()
            .SingleOrDefault(fk => fk.Properties.Count == 1 && fk.Properties[0].Name == nameof(Wishlists.ProductId));
        productFk.ShouldNotBeNull();
        productFk!.DeleteBehavior.ShouldBe(DeleteBehavior.Cascade);
        productFk.IsRequired.ShouldBeTrue();
    }

    [Fact]
    public void Model_HasQueryFilter()
    {
        var entityType = Context.Model.FindEntityType(typeof(Wishlists));
        entityType.ShouldNotBeNull();
        entityType!.GetQueryFilter().ShouldNotBeNull();
    }
}
