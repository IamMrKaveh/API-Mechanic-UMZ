using Infrastructure.Brand.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Brand.Configurations;

[Collection(nameof(DatabaseCollection))]
public class BrandConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task Persist_DuplicateSlug_ThrowsDbUpdateException()
    {
        var sharedSlug = "shared-brand-slug";

        var brandOne = await new BrandBuilder()
            .WithName("First Brand")
            .WithSlug(sharedSlug)
            .BuildAsync();
        brandOne.ClearDomainEvents();

        var brandTwo = await new BrandBuilder()
            .WithName("Second Brand")
            .WithSlug(sharedSlug)
            .BuildAsync();
        brandTwo.ClearDomainEvents();

        await _context.Brands.AddAsync(brandOne);
        await _context.SaveChangesAsync();

        await _context.Brands.AddAsync(brandTwo);

        await Should.ThrowAsync<DbUpdateException>(async () =>
            await _context.SaveChangesAsync());
    }

    [Fact]
    public async Task Persist_Brand_StoresOwnedNameAndSlugAsColumnsAndCanBeQueriedByThem()
    {
        var brand = await new BrandBuilder()
            .WithName("Owned Types Brand")
            .WithSlug("owned-types-brand")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var byName = await freshContext.Brands
            .Where(b => b.Name.Value == "Owned Types Brand")
            .FirstOrDefaultAsync();

        var bySlug = await freshContext.Brands
            .Where(b => b.Slug.Value == "owned-types-brand")
            .FirstOrDefaultAsync();

        byName.ShouldNotBeNull();
        byName!.Id.ShouldBe(brand.Id);
        bySlug.ShouldNotBeNull();
        bySlug!.Id.ShouldBe(brand.Id);
    }

    [Fact]
    public async Task Persist_Brand_XminConcurrencyTokenIsRefreshedOnInsertAndUpdate()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        var repo = new BrandRepository(_context);
        await repo.AddAsync(brand);
        await _context.SaveChangesAsync();

        var initialRowVersion = repo.GetCurrentRowVersion(brand);
        initialRowVersion.ShouldNotBeNull();
        initialRowVersion!.Length.ShouldBe(sizeof(uint));

        brand.Deactivate();
        brand.ClearDomainEvents();
        repo.Update(brand);
        await _context.SaveChangesAsync();

        var updatedRowVersion = repo.GetCurrentRowVersion(brand);
        updatedRowVersion.ShouldNotBeNull();
        updatedRowVersion!.ShouldNotBe(initialRowVersion);
    }
}
