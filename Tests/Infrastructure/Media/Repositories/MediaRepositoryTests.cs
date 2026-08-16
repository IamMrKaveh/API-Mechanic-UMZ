using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Media.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Medias = Domain.Media.Aggregates.Media;

namespace Tests.Infrastructure.Media.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class MediaRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private MediaRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new MediaRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Medias> PersistAsync(Medias media)
    {
        await _sut.AddAsync(media);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return media;
    }

    private async Task SoftDeleteAsync(MediaId id, UserId? deletedBy = null)
    {
        var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Medias.FirstAsync(m => m.Id == id);
        tracked.RequestDeletion(deletedBy);
        deletionContext.Medias.Update(tracked);
        await deletionContext.SaveChangesAsync();
        await deletionContext.DisposeAsync();
    }

    [SkippableFact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsAggregateWithOwnedValueObjects()
    {
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder()
            .WithFilePath("uploads/products/photo.png")
            .WithFileName("photo.png")
            .WithFileType("image/png")
            .WithFileSize(2048)
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .WithSortOrder(3)
            .WithAltText("primary photo")
            .Build();

        await PersistAsync(media);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var loaded = await sut.GetByIdAsync(media.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(media.Id);
        loaded.EntityType.ShouldBe("Product");
        loaded.EntityId.ShouldBe(entityId);
        loaded.FileType.ShouldBe("image/png");
        loaded.SortOrder.ShouldBe(3);
        loaded.AltText.ShouldBe("primary photo");
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDeleted.ShouldBeFalse();
        loaded.Path.Value.ShouldBe("uploads/products/photo.png");
        loaded.Path.FileName.ShouldBe("photo.png");
        loaded.Path.Extension.ShouldBe("png");
        loaded.Size.Bytes.ShouldBe(2048);
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenMediaDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(MediaId.NewId());

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByIdAsync_WhenMediaIsSoftDeleted_ReturnsNullBecauseOfQueryFilter()
    {
        var media = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(media);

        await SoftDeleteAsync(media.Id, UserId.NewId());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var loaded = await sut.GetByIdAsync(media.Id);

        loaded.ShouldBeNull();
    }

    [SkippableFact]
    public async Task Update_AfterSetAsPrimary_PersistsPrimaryFlag()
    {
        var media = new MediaBuilder().WithIsPrimary(false).Build();
        await PersistAsync(media);

        var mutationContext = _fixture.CreateContext();
        var mutationRepo = new MediaRepository(mutationContext);
        var tracked = await mutationContext.Medias.FirstAsync(m => m.Id == media.Id);
        tracked.SetAsPrimary();
        mutationRepo.Update(tracked);
        await mutationContext.SaveChangesAsync();
        await mutationContext.DisposeAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var loaded = await sut.GetByIdAsync(media.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsPrimary.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task GetByEntityAsync_WithMultipleMediasForSameEntity_ReturnsOrderedBySortOrder()
    {
        var entityId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(2).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var result = await sut.GetByEntityAsync("Product", entityId);

        result.Count.ShouldBe(3);
        result.Select(m => m.SortOrder).ToList().ShouldBe(new[] { 0, 1, 2 });
    }

    [SkippableFact]
    public async Task GetByEntityAsync_FiltersByEntityTypeAndEntityId()
    {
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(productId).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(Guid.NewGuid()).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Brand").WithEntityId(brandId).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var productMedias = await sut.GetByEntityAsync("Product", productId);
        var brandMedias = await sut.GetByEntityAsync("Brand", brandId);

        productMedias.Count.ShouldBe(1);
        productMedias.Single().EntityId.ShouldBe(productId);
        brandMedias.Count.ShouldBe(1);
        brandMedias.Single().EntityId.ShouldBe(brandId);
    }

    [SkippableFact]
    public async Task GetByEntityAsync_ExcludesSoftDeletedMedia()
    {
        var entityId = Guid.NewGuid();

        var alive = await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).Build());
        var deletedMedia = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build();
        await PersistAsync(deletedMedia);

        await SoftDeleteAsync(deletedMedia.Id);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var result = await sut.GetByEntityAsync("Product", entityId);

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(alive.Id);
    }

    [SkippableFact]
    public async Task GetPrimaryByEntityAsync_WhenPrimaryExists_ReturnsPrimary()
    {
        var entityId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(0).WithIsPrimary(false).Build());
        var primary = new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithSortOrder(1).Build();
        primary.SetAsPrimary();
        await PersistAsync(primary);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var result = await sut.GetPrimaryByEntityAsync("Product", entityId);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(primary.Id);
        result.IsPrimary.ShouldBeTrue();
    }

    [SkippableFact]
    public async Task GetPrimaryByEntityAsync_WhenNoPrimaryExists_ReturnsNull()
    {
        var entityId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(entityId).WithIsPrimary(false).Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var result = await sut.GetPrimaryByEntityAsync("Product", entityId);

        result.ShouldBeNull();
    }

    [SkippableFact]
    public async Task GetByPathAsync_MatchesMediaByOwnedFilePathValue()
    {
        var uniquePath = $"uploads/products/{Guid.NewGuid():N}.png";
        var fileName = uniquePath.Substring(uniquePath.LastIndexOf('/') + 1);

        await PersistAsync(new MediaBuilder()
            .WithFilePath(uniquePath)
            .WithFileName(fileName)
            .WithEntityType("Product")
            .Build());

        await PersistAsync(new MediaBuilder().WithEntityType("Product").Build());

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var result = await sut.GetByPathAsync(uniquePath);

        result.Count.ShouldBe(1);
        result.Single().Path.Value.ShouldBe(uniquePath);
    }

    [SkippableFact]
    public async Task GetByPathAsync_WhenNoMatch_ReturnsEmpty()
    {
        await PersistAsync(new MediaBuilder().WithEntityType("Product").Build());

        var result = await _sut.GetByPathAsync("uploads/no/such/path.png");

        result.ShouldBeEmpty();
    }

    [SkippableFact]
    public async Task GetAllFilePathsAsync_IncludesSoftDeletedMediaPathsViaIgnoreQueryFilters()
    {
        var aliveFileName = $"{Guid.NewGuid():N}.png";
        var deletedFileName = $"{Guid.NewGuid():N}.png";
        var alivePath = $"uploads/products/{aliveFileName}";
        var deletedPath = $"uploads/products/{deletedFileName}";

        await PersistAsync(new MediaBuilder()
            .WithFilePath(alivePath)
            .WithFileName(aliveFileName)
            .WithEntityType("Product")
            .Build());

        var deletedMedia = new MediaBuilder()
            .WithFilePath(deletedPath)
            .WithFileName(deletedFileName)
            .WithEntityType("Product")
            .Build();
        await PersistAsync(deletedMedia);

        await SoftDeleteAsync(deletedMedia.Id);

        await using var queryContext = _fixture.CreateContext();
        var sut = new MediaRepository(queryContext);

        var paths = await sut.GetAllFilePathsAsync();

        paths.ShouldContain(alivePath);
        paths.ShouldContain(deletedPath);
    }
}
