using Domain.Category.ValueObjects;
using Infrastructure.Category.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Tests.TestInfrastructure.Stubs;

namespace Tests.Infrastructure.Category.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CategoryRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private CategoryRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new CategoryRepository(_context);
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
    public async Task GetByIdAsync_ExistingCategory_ReturnsAggregate()
    {
        var category = await new CategoryBuilder()
            .WithName("Motor Oil")
            .WithSlug("motor-oil-repo-get")
            .WithDescription("engine lubricants")
            .WithSortOrder(3)
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByIdAsync(category.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(category.Id);
        result.Name.Value.ShouldBe("Motor Oil");
        result.Slug.Value.ShouldBe("motor-oil-repo-get");
        result.Description.ShouldBe("engine lubricants");
        result.SortOrder.ShouldBe(3);
        result.IsActive.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_NonExistentCategory_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(CategoryId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_NameExists_ReturnsTrue()
    {
        var category = await new CategoryBuilder()
            .WithName("Brakes")
            .WithSlug("brakes-exists-name")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync(CategoryName.Create("Brakes"));

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_NameDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.ExistsByNameAsync(CategoryName.Create("Absent Category"));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameAsync_NameExistsButExcluded_ReturnsFalse()
    {
        var category = await new CategoryBuilder()
            .WithName("Suspension")
            .WithSlug("suspension-exclude-self")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByNameAsync(CategoryName.Create("Suspension"), category.Id);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_SlugExists_ReturnsTrue()
    {
        var category = await new CategoryBuilder()
            .WithName("Exhaust")
            .WithSlug("exhaust-exists-slug")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsBySlugAsync(CategorySlug.Create("exhaust-exists-slug"));

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_SlugDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.ExistsBySlugAsync(CategorySlug.Create("absent-slug-xyz"));

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_SlugExistsButExcluded_ReturnsFalse()
    {
        var category = await new CategoryBuilder()
            .WithName("Cooling")
            .WithSlug("cooling-exclude-self")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsBySlugAsync(CategorySlug.Create("cooling-exclude-self"), category.Id);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AddAsync_PersistsCategoryAndAllowsRetrievalByRepository()
    {
        var category = await new CategoryBuilder()
            .WithName("Transmission")
            .WithSlug("transmission-add")
            .BuildAsync();

        await _sut.AddAsync(category);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(category.Id);

        reloaded.ShouldNotBeNull();
        reloaded.Id.ShouldBe(category.Id);
        reloaded.Name.Value.ShouldBe("Transmission");
        reloaded.Slug.Value.ShouldBe("transmission-add");
    }

    [RequiresDockerFact]
    public async Task Update_WithStaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        var category = await new CategoryBuilder()
            .WithName("Filters")
            .WithSlug("filters-concurrency")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var staleRowVersion = _context.Entry(category)
            .Property<byte[]>("RowVersion")
            .CurrentValue!
            .ToArray();

        _context.ChangeTracker.Clear();

        await using (var otherContext = _fixture.CreateContext())
        {
            var fresh = await otherContext.Categories.FirstAsync(c => c.Id == category.Id);
            await fresh.UpdateDetails(
                CategoryName.Create("Filters Updated"),
                CategorySlug.Create("filters-concurrency-updated"),
                new StubCategoryUniquenessChecker(),
                "updated description",
                1,
                CancellationToken.None);
            await otherContext.SaveChangesAsync();
        }

        var loaded = await _context.Categories.FirstAsync(c => c.Id == category.Id);
        await loaded.UpdateDetails(
            CategoryName.Create("Filters Conflict"),
            CategorySlug.Create("filters-conflict"),
            new StubCategoryUniquenessChecker(),
            "conflict description",
            2,
            CancellationToken.None);

        _sut.Update(loaded, staleRowVersion);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => _context.SaveChangesAsync());
    }

    [RequiresDockerFact]
    public async Task Update_WithCurrentRowVersion_PersistsChanges()
    {
        var category = await new CategoryBuilder()
            .WithName("Ignition")
            .WithSlug("ignition-update-ok")
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var currentRowVersion = _context.Entry(category)
            .Property<byte[]>("RowVersion")
            .CurrentValue!
            .ToArray();

        _context.ChangeTracker.Clear();

        var loaded = await _context.Categories.FirstAsync(c => c.Id == category.Id);
        await loaded.UpdateDetails(
            CategoryName.Create("Ignition Systems"),
            CategorySlug.Create("ignition-update-ok-v2"),
            new StubCategoryUniquenessChecker(),
            "spark and coils",
            9,
            CancellationToken.None);

        _sut.Update(loaded, currentRowVersion);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(category.Id);
        reloaded.ShouldNotBeNull();
        reloaded.Name.Value.ShouldBe("Ignition Systems");
        reloaded.Slug.Value.ShouldBe("ignition-update-ok-v2");
        reloaded.Description.ShouldBe("spark and coils");
        reloaded.SortOrder.ShouldBe(9);
    }
}
