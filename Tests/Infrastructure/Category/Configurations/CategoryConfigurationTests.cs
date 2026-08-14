using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Category.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CategoryConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    [RequiresDockerFact]
    public async Task SaveChanges_DuplicateCategoryName_ThrowsDbUpdateException()
    {
        var first = await new CategoryBuilder()
            .WithName("Unique Name Test")
            .WithSlug("unique-name-test-a")
            .BuildAsync();

        var second = await new CategoryBuilder()
            .WithName("Unique Name Test")
            .WithSlug("unique-name-test-b")
            .BuildAsync();

        _context.Categories.Add(first);
        await _context.SaveChangesAsync();

        _context.Categories.Add(second);

        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task SaveChanges_DuplicateCategorySlug_ThrowsDbUpdateException()
    {
        var first = await new CategoryBuilder()
            .WithName("Slug Uniqueness A")
            .WithSlug("shared-slug-conflict")
            .BuildAsync();

        var second = await new CategoryBuilder()
            .WithName("Slug Uniqueness B")
            .WithSlug("shared-slug-conflict")
            .BuildAsync();

        _context.Categories.Add(first);
        await _context.SaveChangesAsync();

        _context.Categories.Add(second);

        await Should.ThrowAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task SaveChanges_PersistsAllScalarPropertiesAndRoundTripsCategoryId()
    {
        var category = await new CategoryBuilder()
            .WithName("Roundtrip Cat")
            .WithSlug("roundtrip-cat")
            .WithDescription("full roundtrip description")
            .WithSortOrder(11)
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.Categories.FirstOrDefaultAsync(c => c.Id == category.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(category.Id);
        reloaded.Name.Value.ShouldBe("Roundtrip Cat");
        reloaded.Slug.Value.ShouldBe("roundtrip-cat");
        reloaded.Description.ShouldBe("full roundtrip description");
        reloaded.SortOrder.ShouldBe(11);
        reloaded.IsActive.ShouldBeTrue();
        reloaded.CreatedAt.ShouldNotBe(default);
        reloaded.UpdatedAt.ShouldNotBe(default);

        var rowVersion = _context.Entry(reloaded).Property<byte[]>("RowVersion").CurrentValue;
        rowVersion.ShouldNotBeNull();
        rowVersion.Length.ShouldBeGreaterThan(0);
    }

    [RequiresDockerFact]
    public async Task SaveChanges_UpdatingCategory_ChangesRowVersion()
    {
        var category = await new CategoryBuilder()
            .WithName("RowVersion Cat")
            .WithSlug("rowversion-cat")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var initialRowVersion = _context.Entry(category).Property<byte[]>("RowVersion").CurrentValue!.ToArray();

        category.Activate();
        category.Deactivate();
        await _context.SaveChangesAsync();

        var updatedRowVersion = _context.Entry(category).Property<byte[]>("RowVersion").CurrentValue!;

        updatedRowVersion.SequenceEqual(initialRowVersion).ShouldBeFalse();
    }
}
