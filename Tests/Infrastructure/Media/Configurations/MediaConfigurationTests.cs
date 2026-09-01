using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;
using Medias = Domain.Media.Aggregates.Media;

namespace Tests.Infrastructure.Media.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class MediaConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
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
        media.ClearDomainEvents();
        await _context.Medias.AddAsync(media);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return media;
    }

    [Fact]
    public async Task SaveChanges_ThenReload_RoundTripsMediaIdConversion()
    {
        var media = new MediaBuilder()
            .WithEntityType("Product")
            .WithFilePath("uploads/products/photo.png")
            .WithFileName("photo.png")
            .WithFileType("image/png")
            .Build();

        await PersistAsync(media);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.Id.ShouldBe(media.Id);
        loaded.Id.Value.ShouldBe(media.Id.Value);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_MapsOwnedFilePathToFilePathAndFileNameAndExtensionColumns()
    {
        var media = new MediaBuilder()
            .WithFilePath("uploads/brands/logo.png")
            .WithFileName("logo.png")
            .WithFileType("image/png")
            .WithEntityType("Brand")
            .Build();

        await PersistAsync(media);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.Path.Value.ShouldBe("uploads/brands/logo.png");
        loaded.Path.FileName.ShouldBe("logo.png");
        loaded.Path.Extension.ShouldBe("png");
    }

    [Fact]
    public async Task SaveChanges_ThenReload_MapsOwnedFileSizeToFileSizeColumn()
    {
        var media = new MediaBuilder()
            .WithFileSize(4096)
            .WithEntityType("Product")
            .Build();

        await PersistAsync(media);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.Size.Bytes.ShouldBe(4096);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesAllScalarProperties()
    {
        var entityId = Guid.NewGuid();
        var media = new MediaBuilder()
            .WithEntityType("Product")
            .WithEntityId(entityId)
            .WithFileType("image/webp")
            .WithSortOrder(7)
            .WithAltText("primary product image")
            .Build();

        await PersistAsync(media);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.EntityType.ShouldBe("Product");
        loaded.EntityId.ShouldBe(entityId);
        loaded.FileType.ShouldBe("image/webp");
        loaded.SortOrder.ShouldBe(7);
        loaded.AltText.ShouldBe("primary product image");
        loaded.IsActive.ShouldBeTrue();
        loaded.IsPrimary.ShouldBeFalse();
        loaded.IsDeleted.ShouldBeFalse();
        loaded.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public async Task Ignore_ExposedComputedProperties_AreNotPersistedAsSeparateColumns()
    {
        var media = new MediaBuilder()
            .WithFilePath("uploads/products/photo.png")
            .WithFileName("photo.png")
            .WithFileSize(1024)
            .WithFileType("image/png")
            .WithEntityType("Product")
            .Build();

        await PersistAsync(media);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.FilePath.ShouldBe(loaded.Path.Value);
        loaded.FileName.ShouldBe(loaded.Path.FileName);
        loaded.Extension.ShouldBe(loaded.Path.Extension);
        loaded.FileSize.ShouldBe(loaded.Size.Bytes);
    }

    [Fact]
    public async Task QueryFilter_WithSoftDeletedMedia_ExcludesFromDefaultQuery()
    {
        var media = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(media);

        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Medias.FirstAsync(m => m.Id == media.Id);
        tracked.RequestDeletion(UserId.NewId());
        deletionContext.Medias.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visible = await freshContext.Medias.FirstOrDefaultAsync(m => m.Id == media.Id);

        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WithSoftDeletedMedia_ReturnsSoftDeletedRow()
    {
        var media = new MediaBuilder().WithEntityType("Product").Build();
        await PersistAsync(media);

        var deletedBy = UserId.NewId();
        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Medias.FirstAsync(m => m.Id == media.Id);
        tracked.RequestDeletion(deletedBy);
        deletionContext.Medias.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == media.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDeleted.ShouldBeTrue();
        loaded.IsActive.ShouldBeFalse();
        loaded.DeletedAt.ShouldNotBeNull();
        loaded.DeletedBy.ShouldBe(deletedBy.Value);
    }

    [Fact]
    public async Task Query_ByEntityTypeAndEntityIdCompositeIndex_ReturnsMatchingRows()
    {
        var productId = Guid.NewGuid();
        var brandId = Guid.NewGuid();

        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(productId).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Product").WithEntityId(productId).Build());
        await PersistAsync(new MediaBuilder().WithEntityType("Brand").WithEntityId(brandId).Build());

        await using var freshContext = _fixture.CreateContext();
        var productMedias = await freshContext.Medias
            .Where(m => m.EntityType == "Product" && m.EntityId == productId)
            .ToListAsync();
        var brandMedias = await freshContext.Medias
            .Where(m => m.EntityType == "Brand" && m.EntityId == brandId)
            .ToListAsync();

        productMedias.Count.ShouldBe(2);
        brandMedias.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Update_AfterSetAsPrimary_PersistsPrimaryFlag()
    {
        var media = new MediaBuilder().WithEntityType("Product").WithIsPrimary(false).Build();
        await PersistAsync(media);

        await using var mutationContext = _fixture.CreateContext();
        var tracked = await mutationContext.Medias.FirstAsync(m => m.Id == media.Id);
        tracked.SetAsPrimary();
        mutationContext.Medias.Update(tracked);
        await mutationContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Medias.FirstAsync(m => m.Id == media.Id);

        loaded.IsPrimary.ShouldBeTrue();
        loaded.UpdatedAt.ShouldNotBeNull();
    }
}
