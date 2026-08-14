using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Infrastructure.Brand.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Brand.Repositories;

[Collection(nameof(DatabaseCollection))]
public class BrandRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private BrandRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new BrandRepository(_context);
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
    public async Task GetByIdAsync_ExistingBrand_ReturnsBrand()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var loaded = await _sut.GetByIdAsync(brand.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(brand.Id);
        loaded.Name.Value.ShouldBe(brand.Name.Value);
        loaded.Slug.Value.ShouldBe(brand.Slug.Value);
        loaded.CategoryId.ShouldBe(brand.CategoryId);
    }

    [RequiresDockerFact]
    public async Task GetByIdAsync_NonExistentBrand_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(BrandId.NewId());

        loaded.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameInCategoryAsync_MatchingNameAndCategory_ReturnsTrue()
    {
        var categoryId = CategoryId.NewId();
        var brand = await new BrandBuilder()
            .WithName("Contoso Industries")
            .WithCategoryId(categoryId)
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var exists = await _sut.ExistsByNameInCategoryAsync(brand.Name, categoryId);

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameInCategoryAsync_NameInDifferentCategory_ReturnsFalse()
    {
        var categoryA = CategoryId.NewId();
        var categoryB = CategoryId.NewId();

        var brand = await new BrandBuilder()
            .WithName("Fabrikam Corp")
            .WithCategoryId(categoryA)
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var exists = await _sut.ExistsByNameInCategoryAsync(brand.Name, categoryB);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsByNameInCategoryAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var categoryId = CategoryId.NewId();
        var brand = await new BrandBuilder()
            .WithName("Northwind Traders")
            .WithCategoryId(categoryId)
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var exists = await _sut.ExistsByNameInCategoryAsync(brand.Name, categoryId, brand.Id);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_MatchingSlug_ReturnsTrue()
    {
        var brand = await new BrandBuilder()
            .WithSlug("contoso-industries")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var exists = await _sut.ExistsBySlugAsync(brand.Slug);

        exists.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_NonMatchingSlug_ReturnsFalse()
    {
        var brand = await new BrandBuilder()
            .WithSlug("fabrikam-corp")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var otherSlug = BrandSlug.Create("some-other-slug");
        var exists = await _sut.ExistsBySlugAsync(otherSlug);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task ExistsBySlugAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var brand = await new BrandBuilder()
            .WithSlug("adventure-works")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var exists = await _sut.ExistsBySlugAsync(brand.Slug, brand.Id);

        exists.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task AddAsync_ThenSave_PersistsBrandAcrossContexts()
    {
        var brand = await new BrandBuilder()
            .WithName("Litware Inc")
            .WithSlug("litware-inc")
            .WithDescription("A litware description.")
            .WithLogoPath("brands/litware.png")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new BrandRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(brand.Id);

        loaded.ShouldNotBeNull();
        loaded!.Name.Value.ShouldBe("Litware Inc");
        loaded.Slug.Value.ShouldBe("litware-inc");
        loaded.Description.ShouldBe("A litware description.");
        loaded.LogoPath.ShouldBe("brands/litware.png");
        loaded.IsActive.ShouldBeTrue();
        loaded.CategoryId.ShouldBe(brand.CategoryId);
    }

    [RequiresDockerFact]
    public async Task Update_ChangeCategory_PersistsAcrossContexts()
    {
        var originalCategory = CategoryId.NewId();
        var newCategory = CategoryId.NewId();

        var brand = await new BrandBuilder()
            .WithCategoryId(originalCategory)
            .BuildAsync();
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        brand.ChangeCategory(newCategory);
        brand.ClearDomainEvents();
        _sut.Update(brand);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new BrandRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(brand.Id);

        loaded.ShouldNotBeNull();
        loaded!.CategoryId.ShouldBe(newCategory);
    }

    [RequiresDockerFact]
    public async Task Update_Deactivate_PersistsIsActiveFalse()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        brand.Deactivate();
        brand.ClearDomainEvents();
        _sut.Update(brand);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new BrandRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(brand.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsActive.ShouldBeFalse();
    }

    [RequiresDockerFact]
    public async Task GetCurrentRowVersion_AfterSave_ReturnsFourByteToken()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        var rowVersion = _sut.GetCurrentRowVersion(brand);

        rowVersion.ShouldNotBeNull();
        rowVersion!.Length.ShouldBe(sizeof(uint));
    }

    [RequiresDockerFact]
    public async Task GetCurrentRowVersion_AfterUpdate_ChangesFromPreviousValue()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        var beforeUpdate = _sut.GetCurrentRowVersion(brand);
        beforeUpdate.ShouldNotBeNull();

        brand.Deactivate();
        brand.ClearDomainEvents();
        _sut.Update(brand);
        await _context.SaveChangesAsync();

        var afterUpdate = _sut.GetCurrentRowVersion(brand);
        afterUpdate.ShouldNotBeNull();
        afterUpdate!.ShouldNotBe(beforeUpdate!);
    }

    [RequiresDockerFact]
    public async Task SaveChanges_WithStaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        var brand = await new BrandBuilder().BuildAsync();
        var brandId = brand.Id;
        brand.ClearDomainEvents();

        await _sut.AddAsync(brand);
        await _context.SaveChangesAsync();

        var staleRowVersion = _sut.GetCurrentRowVersion(brand);
        staleRowVersion.ShouldNotBeNull();

        await using (var mutatingContext = _fixture.CreateContext())
        {
            var mutatingRepo = new BrandRepository(mutatingContext);
            var mutating = await mutatingRepo.GetByIdAsync(brandId);
            mutating.ShouldNotBeNull();
            mutating!.Deactivate();
            mutating.ClearDomainEvents();
            await mutatingContext.SaveChangesAsync();
        }

        await using var conflictingContext = _fixture.CreateContext();
        var conflictingRepo = new BrandRepository(conflictingContext);
        var conflicting = await conflictingRepo.GetByIdAsync(brandId);
        conflicting.ShouldNotBeNull();

        conflicting!.ChangeCategory(CategoryId.NewId());
        conflicting.ClearDomainEvents();
        conflictingRepo.SetOriginalRowVersion(conflicting, staleRowVersion!);

        await Should.ThrowAsync<DbUpdateConcurrencyException>(async () =>
            await conflictingContext.SaveChangesAsync());
    }
}
