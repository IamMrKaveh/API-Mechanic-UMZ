using Application.Common.Contracts;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Wishlist.QueryServices;

using Tests.TestInfrastructure.Builders;
using WishlistAggregate = Domain.Wishlist.Aggregates.Wishlist;

namespace Tests.Infrastructure.Wishlist.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class WishlistQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IUrlResolverService _urlResolver = null!;
    private WishlistQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver.ResolveMediaUrl(Arg.Any<string>()).Returns(c => $"https://cdn.test/{c.Arg<string>()}");
        _sut = new WishlistQueryService(_context, _urlResolver);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(UserId userId, ProductId productId, string productName)> SeedUserAndProductAsync(
        UserId? userId = null,
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

        var resolvedName = productName ?? $"Product-{Guid.NewGuid():N}";

        var product = new ProductBuilder()
            .WithName(resolvedName)
            .WithSlug($"prod-{Guid.NewGuid():N}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        _context.Products.Add(product);

        UserId resolvedUserId;
        if (userId is null)
        {
            var user = new UserBuilder().Build();
            _context.Users.Add(user);
            resolvedUserId = user.Id;
        }
        else
        {
            resolvedUserId = userId;
        }

        await _context.SaveChangesAsync();

        return (resolvedUserId, product.Id, resolvedName);
    }

    private async Task<WishlistAggregate> PersistWishlistAsync(UserId userId, ProductId productId)
    {
        var wishlist = new WishlistBuilder()
            .WithUserId(userId)
            .WithProductId(productId)
            .Build();

        _context.Wishlists.Add(wishlist);
        await _context.SaveChangesAsync();
        return wishlist;
    }

    [Fact]
    public async Task GetPagedAsync_EmptyWishlist_ReturnsEmptyPaginatedResult()
    {
        var result = await _sut.GetPagedAsync(UserId.NewId(), page: 1, pageSize: 10, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetPagedAsync_WithItems_ReturnsPagedDtosWithProductName()
    {
        var (userId, productId, productName) = await SeedUserAndProductAsync(productName: "Brake Pad");
        await PersistWishlistAsync(userId, productId);

        var result = await _sut.GetPagedAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);

        var dto = result.Items.Single();
        dto.ProductId.ShouldBe(productId.Value);
        dto.ProductName.ShouldBe("Brake Pad");
        dto.MinPrice.ShouldBe(0m);
        dto.IsInStock.ShouldBeFalse();
        dto.IconUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_WithPrimaryActiveMedia_ResolvesIconUrl()
    {
        var (userId, productId, _) = await SeedUserAndProductAsync();
        var wishlist = await PersistWishlistAsync(userId, productId);

        var media = new MediaBuilder()
            .WithFilePath("uploads/products/icon.png")
            .WithFileName("icon.png")
            .WithEntityType("Product")
            .WithEntityId(productId.Value)
            .BuildPrimary();
        _context.Medias.Add(media);
        await _context.SaveChangesAsync();

        var result = await _sut.GetPagedAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

        var dto = result.Items.Single(i => i.Id == wishlist.Id.Value);
        dto.IconUrl.ShouldBe("https://cdn.test/uploads/products/icon.png");
    }

    [Fact]
    public async Task GetPagedAsync_WithNoPrimaryMedia_ReturnsNullIconUrl()
    {
        var (userId, productId, _) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId);

        var media = new MediaBuilder()
            .WithFilePath("uploads/products/other.png")
            .WithFileName("other.png")
            .WithEntityType("Product")
            .WithEntityId(productId.Value)
            .WithIsPrimary(false)
            .Build();
        _context.Medias.Add(media);
        await _context.SaveChangesAsync();

        var result = await _sut.GetPagedAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

        result.Items.Single().IconUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetPagedAsync_OrderedByCreatedAtDescending()
    {
        var (userId, productId1, _) = await SeedUserAndProductAsync();
        var (_, productId2, _) = await SeedUserAndProductAsync(userId);
        var (_, productId3, _) = await SeedUserAndProductAsync(userId);

        var w1 = await PersistWishlistAsync(userId, productId1);
        await Task.Delay(5);
        var w2 = await PersistWishlistAsync(userId, productId2);
        await Task.Delay(5);
        var w3 = await PersistWishlistAsync(userId, productId3);

        var result = await _sut.GetPagedAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        var ids = result.Items.Select(i => i.Id).ToList();
        ids[0].ShouldBe(w3.Id.Value);
        ids[1].ShouldBe(w2.Id.Value);
        ids[2].ShouldBe(w1.Id.Value);
    }

    [Fact]
    public async Task GetPagedAsync_WithPaging_ReturnsCorrectPage()
    {
        var (userId, productId1, _) = await SeedUserAndProductAsync();
        var (_, productId2, _) = await SeedUserAndProductAsync(userId);
        var (_, productId3, _) = await SeedUserAndProductAsync(userId);

        await PersistWishlistAsync(userId, productId1);
        await Task.Delay(5);
        await PersistWishlistAsync(userId, productId2);
        await Task.Delay(5);
        await PersistWishlistAsync(userId, productId3);

        var page1 = await _sut.GetPagedAsync(userId, page: 1, pageSize: 2, CancellationToken.None);
        var page2 = await _sut.GetPagedAsync(userId, page: 2, pageSize: 2, CancellationToken.None);

        page1.TotalCount.ShouldBe(3);
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -5)]
    public async Task GetPagedAsync_WithInvalidPaging_UsesDefaults(int page, int pageSize)
    {
        var (userId, productId, _) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId);

        var result = await _sut.GetPagedAsync(userId, page, pageSize, CancellationToken.None);

        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsOnlyRequestedUserItems()
    {
        var (userId, productId, _) = await SeedUserAndProductAsync();
        var (otherUserId, otherProductId, _) = await SeedUserAndProductAsync();

        await PersistWishlistAsync(userId, productId);
        await PersistWishlistAsync(otherUserId, otherProductId);

        var result = await _sut.GetPagedAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().ProductId.ShouldBe(productId.Value);
    }

    [Fact]
    public async Task IsInWishlistAsync_WhenExists_ReturnsTrue()
    {
        var (userId, productId, _) = await SeedUserAndProductAsync();
        await PersistWishlistAsync(userId, productId);

        var result = await _sut.IsInWishlistAsync(userId, productId, CancellationToken.None);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsInWishlistAsync_WhenNotExists_ReturnsFalse()
    {
        var (userId, _, _) = await SeedUserAndProductAsync();

        var result = await _sut.IsInWishlistAsync(userId, ProductId.NewId(), CancellationToken.None);

        result.ShouldBeFalse();
    }
}
