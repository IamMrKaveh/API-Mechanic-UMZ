using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wishlist.Repositories;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using WishlistAggregate = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Infrastructure.Wishlist.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WishlistRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private WishlistRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new WishlistRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(UserId userId, ProductId productId)> SeedUserAndProductAsync(
        string? productName = null)
    {
        var category = await new CategoryBuilder()
            .WithName($"cat-{Guid.NewGuid():N}")
            .WithSlug($"cat-{Guid.NewGuid():N}")
            .BuildAsync();
        _context.Categories.Add(category);

        var brand = await new BrandBuilder()
            .WithName($"brand-{Guid.NewGuid():N}")
            .WithSlug($"brand-{Guid.NewGuid():N}")
            .WithCategoryId(category.Id)
            .BuildAsync();
        _context.Brands.Add(brand);

        var productBuilder = new ProductBuilder()
            .WithSlug($"prod-{Guid.NewGuid():N}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id);

        if (productName is not null)
            productBuilder = productBuilder.WithName(productName);

        var product = productBuilder.Build();
        _context.Products.Add(product);

        var user = new UserBuilder().Build();
        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        return (user.Id, product.Id);
    }

    private async Task<WishlistAggregate> PersistWishlistAsync(
        UserId? userId = null,
        ProductId? productId = null)
    {
        UserId resolvedUserId;
        ProductId resolvedProductId;

        if (userId is null || productId is null)
        {
            var (seededUserId, seededProductId) = await SeedUserAndProductAsync();
            resolvedUserId = userId ?? seededUserId;
            resolvedProductId = productId ?? seededProductId;
        }
        else
        {
            resolvedUserId = userId;
            resolvedProductId = productId;
        }

        var wishlist = new WishlistBuilder()
            .WithUserId(resolvedUserId)
            .WithProductId(resolvedProductId)
            .Build();

        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();
        return wishlist;
    }

    [RequiresDockerFact]
    public async Task AddAsync_WithValidWishlist_PersistsToDatabase()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        var wishlist = new WishlistBuilder()
            .WithUserId(userId)
            .WithProductId(productId)
            .Build();

        await _sut.AddAsync(wishlist);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Wishlists
            .FirstOrDefaultAsync(w => w.Id == wishlist.Id);

        persisted.ShouldNotBeNull();
        persisted.UserId.ShouldBe(userId);
        persisted.ProductId.ShouldBe(productId);
    }

    [RequiresDockerFact]
    public async Task GetByUserAndProductAsync_WhenExists_ReturnsMatchingWishlist()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        var persisted = await PersistWishlistAsync(userId, productId);

        await using var queryContext = _fixture.CreateContext();
        var sut = new WishlistRepository(queryContext);

        var result = await sut.GetByUserAndProductAsync(userId, productId);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(persisted.Id);
        result.UserId.ShouldBe(userId);
        result.ProductId.ShouldBe(productId);
    }

    [RequiresDockerFact]
    public async Task GetByUserAndProductAsync_WhenDoesNotExist_ReturnsNull()
    {
        var (userId, _) = await SeedUserAndProductAsync();

        var result = await _sut.GetByUserAndProductAsync(userId, ProductId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetByUserAndProductAsync_WhenDifferentUser_ReturnsNull()
    {
        var (_, productId) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(productId: productId);

        var result = await _sut.GetByUserAndProductAsync(UserId.NewId(), productId);

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ExistsAsync_WhenExists_ReturnsTrue()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId);

        await using var queryContext = _fixture.CreateContext();
        var sut = new WishlistRepository(queryContext);

        var result = await sut.ExistsAsync(userId, productId);

        result.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsAsync_WhenDoesNotExist_ReturnsFalse()
    {
        var (userId, _) = await SeedUserAndProductAsync();

        var result = await _sut.ExistsAsync(userId, ProductId.NewId());

        result.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task RemoveAsync_WithExistingWishlist_RemovesFromDatabase()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId);

        await _sut.RemoveAsync(userId, productId);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Wishlists
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

        persisted.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task RemoveAsync_WhenNotExists_DoesNotThrow()
    {
        var (userId, _) = await SeedUserAndProductAsync();

        await Should.NotThrowAsync(async () =>
        {
            await _sut.RemoveAsync(userId, ProductId.NewId());
            await _context.SaveChangesAsync();
        });
    }

    [RequiresDockerFact]
    public async Task ClearAsync_RemovesAllUserWishlistItems()
    {
        var (userId, productId1) = await SeedUserAndProductAsync();
        var (_, productId2) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId1);
        await PersistWishlistAsync(userId, productId2);

        await _sut.ClearAsync(userId);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var remaining = await queryContext.Wishlists
            .Where(w => w.UserId == userId)
            .ToListAsync();

        remaining.ShouldBeEmpty();
    }

    [RequiresDockerFact]
    public async Task ClearAsync_DoesNotRemoveOtherUsersItems()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        var (otherUserId, otherProductId) = await SeedUserAndProductAsync();

        await PersistWishlistAsync(userId, productId);
        await PersistWishlistAsync(otherUserId, otherProductId);

        await _sut.ClearAsync(userId);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var otherRemaining = await queryContext.Wishlists
            .Where(w => w.UserId == otherUserId)
            .ToListAsync();

        otherRemaining.Count.ShouldBe(1);
    }

    [RequiresDockerFact]
    public async Task Update_WithModifiedWishlist_PersistsChanges()
    {
        var (userId, productId) = await SeedUserAndProductAsync();
        var persisted = await PersistWishlistAsync(userId, productId);

        _sut.Update(persisted);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var reloaded = await queryContext.Wishlists
            .FirstOrDefaultAsync(w => w.Id == persisted.Id);

        reloaded.ShouldNotBeNull();
        reloaded.UserId.ShouldBe(userId);
        reloaded.ProductId.ShouldBe(productId);
    }
}
