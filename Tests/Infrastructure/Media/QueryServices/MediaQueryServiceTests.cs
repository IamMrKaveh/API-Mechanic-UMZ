using Application.Common.Contracts;
using Domain.Media.ValueObjects;
using Infrastructure.Media.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Medias = Domain.Media.Aggregates.Media;

namespace Tests.Infrastructure.Media.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class MediaQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private IUrlResolverService _urlResolver = null!; private MediaQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver.ResolveMediaUrl(Arg.Any<string>()).Returns(ci => $"https://cdn.test/{ci.Arg<string>()}");
        _sut = new MediaQueryService(_context, _urlResolver);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task PersistAsync(Medias media)
    {
        _context.Medias.Add(media);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task SoftDeleteAsync(MediaId id)
    {
        var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Medias.FirstAsync(m => m.Id == id);
        tracked.RequestDeletion(null);
        deletionContext.Medias.Update(tracked);
        await deletionContext.SaveChangesAsync();
        await deletionContext.DisposeAsync();
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenMediaExists_ReturnsDtoWithResolvedPublicUrl()
    {
        var media = new MediaBuilder()
            .WithFilePath("uploads/products/hero.png")
            .WithFileName("hero.png")
            .WithFileType("image/png")
            .WithFileSize(4096)
            .WithEntityType("Product")
            .WithSortOrder(5)
            .WithAltText("hero shot")
            .Build();
        await PersistAsync(media);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var dto = await sut.GetByIdAsync(media.Id);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(media.Id.Value);
        dto.FilePath.ShouldBe("uploads/products/hero.png");
        dto.FileName.ShouldBe("hero.png");
        dto.FileType.ShouldBe("image/png");
        dto.FileSize.ShouldBe(4096);
        dto.EntityType.ShouldBe("Product");
        dto.EntityId.ShouldBe(media.EntityId);
        dto.SortOrder.ShouldBe(5);
        dto.AltText.ShouldBe("hero shot");
        dto.IsActive.ShouldBeTrue();
        dto.PublicUrl.ShouldBe("https://cdn.test/uploads/products/hero.png");
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenMediaDoesNotExist_ReturnsNull()
    {
        var dto = await _sut.GetByIdAsync(MediaId.NewId());

        dto.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenMediaIsSoftDeleted_ReturnsNull()
    {
        var media = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(media);

        await SoftDeleteAsync(media.Id);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var dto = await sut.GetByIdAsync(media.Id);

        dto.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByEntityAsync_ReturnsMediasOrderedBySortOrder()
    {
        var entityId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(2).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var result = await sut.GetByEntityAsync("Product", entityId);

        result.Count.ShouldBe(3);
        result.Select(dto => dto.SortOrder).ToList().ShouldBe(new[] { 0, 1, 2 });
    }

    [SkippableFact]
    public async Task GetByEntityAsync_ProjectsPublicUrlUsingUrlResolver()
    {
        var entityId = Guid.NewGuid();
        await PersistAsync(new MediaBuilder()
            .WithFilePath("uploads/products/a.png")
            .WithFileName("a.png")
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var result = await sut.GetByEntityAsync("Product", entityId);

        result.Single().PublicUrl.ShouldBe("https://cdn.test/uploads/products/a.png");
    }

    [SkippableFact]
    public async Task GetPrimaryByEntityAsync_WhenPrimaryExists_ReturnsPrimaryDto()
    {
        var entityId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).WithIsPrimary(false).Build());
        var primary = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build();
        primary.SetAsPrimary();
        await PersistAsync(primary);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var dto = await sut.GetPrimaryByEntityAsync("Product", entityId);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(primary.Id.Value);
        dto.IsPrimary.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task GetPrimaryByEntityAsync_WhenNoPrimary_ReturnsNull()
    {
        var entityId = Guid.NewGuid();
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithIsPrimary(false).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var dto = await sut.GetPrimaryByEntityAsync("Product", entityId);

        dto.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetPrimaryByEntitiesAsync_ReturnsDictionaryKeyedByEntityId()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var productC = Guid.NewGuid();

        var primaryA = new MediaBuilder().WithEntityType("Product").WithEntityId(productA).Build();
        primaryA.SetAsPrimary();
        await PersistAsync(primaryA);

        var primaryB = new MediaBuilder().WithEntityType("Product").WithEntityId(productB).Build();
        primaryB.SetAsPrimary();
        await PersistAsync(primaryB);

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(productC).WithIsPrimary(false).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var result = await sut.GetPrimaryByEntitiesAsync("Product", new[] { productA, productB, productC });

        result.Count.ShouldBe(2);
        result.ContainsKey(productA).ShouldBeTrue();
        result.ContainsKey(productB).ShouldBeTrue();
        result.ContainsKey(productC).ShouldBeFalse();
        result[productA].Id.ShouldBe(primaryA.Id.Value);
        result[productB].Id.ShouldBe(primaryB.Id.Value);
    }

    [SkippableFact]
    public async Task GetPrimaryByEntitiesAsync_WithEmptyEntityIds_ReturnsEmptyDictionary()
    {
        var result = await _sut.GetPrimaryByEntitiesAsync("Product", Array.Empty<Guid>());

        result.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task GetPrimaryByEntitiesAsync_DeduplicatesInputEntityIds()
    {
        var productId = Guid.NewGuid();
        var primary = new MediaBuilder().WithEntityType("Product").WithEntityId(productId).Build();
        primary.SetAsPrimary();
        await PersistAsync(primary);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var result = await sut.GetPrimaryByEntitiesAsync("Product", new[] { productId, productId, productId });

        result.Count.ShouldBe(1);
        result[productId].Id.ShouldBe(primary.Id.Value);
    }

    [SkippableFact]
    public async Task GetAllAsync_WithEntityTypeFilter_ReturnsOnlyMatchingEntityType()
    {
        for (var i = 0; i < 3; i++)
            await PersistAsync(new MediaBuilder().WithEntityType("Product").Build());

        await PersistAsync(new MediaBuilder().WithEntityType("Brand").Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var page = await sut.GetAllAsync("Product", page: 1, pageSize: 10);

        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(3);
        page.Items.ShouldAllBe(dto => dto.EntityType == "Product");
    }

    [SkippableFact]
    public async Task GetAllAsync_WithNullEntityTypeFilter_ReturnsAllEntityTypes()
    {
        await PersistAsync(new MediaBuilder().WithEntityType("Product").Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Brand").Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Category").Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var page = await sut.GetAllAsync(entityType: null, page: 1, pageSize: 10);

        page.TotalCount.ShouldBe(3);
        page.Items.Count.ShouldBe(3);
    }

    [SkippableFact]
    public async Task GetAllAsync_AppliesPagingWindow()
    {
        for (var i = 0; i < 5; i++)
            await PersistAsync(new MediaBuilder().WithEntityType("Product").Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var page = await sut.GetAllAsync("Product", page: 2, pageSize: 2);

        page.TotalCount.ShouldBe(5);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.Items.Count.ShouldBe(2);
        page.TotalPages.ShouldBe(3);
    }

    [SkippableFact]
    public async Task GetAllAsync_ExcludesSoftDeletedMedia()
    {
        var alive = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(alive);

        var deletedMedia = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(deletedMedia);

        await SoftDeleteAsync(deletedMedia.Id);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaQueryService(queryContext, _urlResolver);

        var page = await sut.GetAllAsync("Product", page: 1, pageSize: 10);

        page.TotalCount.ShouldBe(1);
        page.Items.Single().Id.ShouldBe(alive.Id.Value);
    }
}
