using Application.Common.Contracts;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Infrastructure.Brand.QueryServices;
using Infrastructure.Brand.Repositories;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Brand.QueryServices;

[Collection(nameof(DatabaseCollection))]
public class BrandQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private IUrlResolverService _urlResolver = null!; private BrandQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver.ResolveMediaUrl(Arg.Any<string>())
            .Returns(ci => ci.Arg<string>() ?? string.Empty);
        _sut = new BrandQueryService(_context, _urlResolver);
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
    public async Task GetBrandDetailAsync_NonExistentBrand_ReturnsNull()
    {
        var dto = await _sut.GetBrandDetailAsync(BrandId.NewId());

        dto.ShouldBeNull();
    }

    [Fact]
    public async Task GetBrandDetailAsync_ExistingBrand_ProjectsBasicFields()
    {
        var brand = await new BrandBuilder()
            .WithName("Acme Corp")
            .WithSlug("acme-corp")
            .WithDescription("Everything under the sun.")
            .WithLogoPath("brands/acme.png")
            .BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var dto = await _sut.GetBrandDetailAsync(brand.Id);

        dto.ShouldNotBeNull();
        dto!.Id.ShouldBe(brand.Id.Value);
        dto.Name.ShouldBe("Acme Corp");
        dto.Slug.ShouldBe("acme-corp");
        dto.Description.ShouldBe("Everything under the sun.");
        dto.CategoryId.ShouldBe(brand.CategoryId.Value);
        dto.IsActive.ShouldBeTrue();
        dto.ProductCount.ShouldBe(0);
        dto.ActiveProductCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetBrandDetailAsync_WithSeededCategory_PopulatesCategoryName()
    {
        var category = await new CategoryBuilder()
            .WithName("Automotive")
            .BuildAsync();
        category.ClearDomainEvents();
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var brand = await new BrandBuilder()
            .WithCategoryId(category.Id)
            .BuildAsync();
        brand.ClearDomainEvents();
        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var dto = await _sut.GetBrandDetailAsync(brand.Id);

        dto.ShouldNotBeNull();
        dto!.CategoryName.ShouldBe("Automotive");
    }

    [Fact]
    public async Task GetBrandDetailAsync_WithoutSeededCategory_ReturnsEmptyCategoryName()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var dto = await _sut.GetBrandDetailAsync(brand.Id);

        dto.ShouldNotBeNull();
        dto!.CategoryName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task GetBrandDetailAsync_ResolvesLogoPathThroughUrlResolver()
    {
        var brand = await new BrandBuilder()
            .WithLogoPath("brands/logo.png")
            .BuildAsync();
        brand.ClearDomainEvents();

        _urlResolver.ClearReceivedCalls();
        _urlResolver.ResolveMediaUrl("brands/logo.png").Returns("https://cdn.test/brands/logo.png");

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var dto = await _sut.GetBrandDetailAsync(brand.Id);

        dto.ShouldNotBeNull();
        dto!.LogoPath.ShouldBe("https://cdn.test/brands/logo.png");
        _urlResolver.Received().ResolveMediaUrl("brands/logo.png");
    }

    [Fact]
    public async Task GetBrandDetailAsync_ReturnsBase64EncodedRowVersion()
    {
        var brand = await new BrandBuilder().BuildAsync();
        brand.ClearDomainEvents();

        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var repo = new BrandRepository(_context);
        var currentRowVersion = repo.GetCurrentRowVersion(brand);
        currentRowVersion.ShouldNotBeNull();

        var dto = await _sut.GetBrandDetailAsync(brand.Id);

        dto.ShouldNotBeNull();
        dto!.RowVersion.ShouldNotBeNullOrEmpty();

        var decoded = Convert.FromBase64String(dto.RowVersion);
        decoded.Length.ShouldBe(sizeof(uint));
        decoded.ShouldBe(currentRowVersion!);
    }

    [Fact]
    public async Task GetBrandsPagedAsync_NoFilters_ReturnsAllBrandsOrderedByName()
    {
        var brandZeta = await new BrandBuilder().WithName("Zeta").WithSlug("zeta").BuildAsync();
        var brandAlpha = await new BrandBuilder().WithName("Alpha").WithSlug("alpha").BuildAsync();
        var brandMike = await new BrandBuilder().WithName("Mike").WithSlug("mike").BuildAsync();
        foreach (var b in new[] { brandZeta, brandAlpha, brandMike })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(brandZeta, brandAlpha, brandMike);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: null,
            search: null,
            isActive: null,
            includeDeleted: false,
            page: 1,
            pageSize: 10);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
        result.Items.Select(i => i.Name).ToList().ShouldBe(new[] { "Alpha", "Mike", "Zeta" });
    }

    [Fact]
    public async Task GetBrandsPagedAsync_WithCategoryFilter_ReturnsBrandsInThatCategoryOnly()
    {
        var categoryA = CategoryId.NewId();
        var categoryB = CategoryId.NewId();

        var brandInA1 = await new BrandBuilder().WithCategoryId(categoryA).WithName("A One").WithSlug("a-one").BuildAsync();
        var brandInA2 = await new BrandBuilder().WithCategoryId(categoryA).WithName("A Two").WithSlug("a-two").BuildAsync();
        var brandInB = await new BrandBuilder().WithCategoryId(categoryB).WithName("B One").WithSlug("b-one").BuildAsync();
        foreach (var b in new[] { brandInA1, brandInA2, brandInB })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(brandInA1, brandInA2, brandInB);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: categoryA,
            search: null,
            isActive: null,
            includeDeleted: false,
            page: 1,
            pageSize: 10);

        result.TotalCount.ShouldBe(2);
        result.Items.Select(i => i.CategoryId).ShouldAllBe(id => id == categoryA.Value);
    }

    [Fact]
    public async Task GetBrandsPagedAsync_WithSearchOnName_MatchesCaseInsensitively()
    {
        var brandOne = await new BrandBuilder().WithName("Contoso Industries").WithSlug("contoso-industries").BuildAsync();
        var brandTwo = await new BrandBuilder().WithName("Fabrikam Corp").WithSlug("fabrikam-corp").BuildAsync();
        foreach (var b in new[] { brandOne, brandTwo })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(brandOne, brandTwo);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: null,
            search: "CONTOSO",
            isActive: null,
            includeDeleted: false,
            page: 1,
            pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Contoso Industries");
    }

    [Fact]
    public async Task GetBrandsPagedAsync_WithSearchOnSlug_MatchesCaseInsensitively()
    {
        var brandOne = await new BrandBuilder().WithName("Northwind").WithSlug("northwind-traders").BuildAsync();
        var brandTwo = await new BrandBuilder().WithName("Adventure Works").WithSlug("adventure-works").BuildAsync();
        foreach (var b in new[] { brandOne, brandTwo })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(brandOne, brandTwo);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: null,
            search: "ADVENTURE",
            isActive: null,
            includeDeleted: false,
            page: 1,
            pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Slug.ShouldBe("adventure-works");
    }

    [Fact]
    public async Task GetBrandsPagedAsync_WithIsActiveFalse_ReturnsOnlyDeactivatedBrands()
    {
        var activeBrand = await new BrandBuilder().WithName("Active Brand").WithSlug("active-brand").BuildAsync();
        var inactiveBrand = await new BrandBuilder().WithName("Inactive Brand").WithSlug("inactive-brand").BuildAsync();
        inactiveBrand.Deactivate();
        activeBrand.ClearDomainEvents();
        inactiveBrand.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(activeBrand, inactiveBrand);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: null,
            search: null,
            isActive: false,
            includeDeleted: false,
            page: 1,
            pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IsActive.ShouldBeFalse();
        result.Items[0].Name.ShouldBe("Inactive Brand");
    }

    [Fact]
    public async Task GetBrandsPagedAsync_WithPagination_ReturnsRequestedSliceOfOrderedResults()
    {
        var brands = new List<global::Domain.Brand.Aggregates.Brand>();
        foreach (var name in new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo" })
        {
            var brand = await new BrandBuilder()
                .WithName(name)
                .WithSlug(name.ToLowerInvariant())
                .BuildAsync();
            brand.ClearDomainEvents();
            brands.Add(brand);
        }

        await _context.Brands.AddRangeAsync(brands);
        await _context.SaveChangesAsync();

        var result = await _sut.GetBrandsPagedAsync(
            categoryId: null,
            search: null,
            isActive: null,
            includeDeleted: false,
            page: 2,
            pageSize: 2);

        result.TotalCount.ShouldBe(5);
        result.Items.Count.ShouldBe(2);
        result.Items.Select(i => i.Name).ToList().ShouldBe(new[] { "Charlie", "Delta" });
        result.Page.ShouldBe(2);
        result.PageSize.ShouldBe(2);
    }

    [Fact]
    public async Task GetPublicBrandsAsync_ReturnsOnlyActiveBrandsOrderedByName()
    {
        var activeBravo = await new BrandBuilder().WithName("Bravo").WithSlug("bravo").BuildAsync();
        var activeAlpha = await new BrandBuilder().WithName("Alpha").WithSlug("alpha").BuildAsync();
        var deactivated = await new BrandBuilder().WithName("Inactive").WithSlug("inactive").BuildAsync();
        deactivated.Deactivate();
        foreach (var b in new[] { activeBravo, activeAlpha, deactivated })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(activeBravo, activeAlpha, deactivated);
        await _context.SaveChangesAsync();

        var result = await _sut.GetPublicBrandsAsync();

        result.Count.ShouldBe(2);
        result.Select(i => i.Name).ToList().ShouldBe(new[] { "Alpha", "Bravo" });
        result.ShouldAllBe(i => i.IsActive);
    }

    [Fact]
    public async Task GetPublicBrandsAsync_WithCategoryFilter_ReturnsOnlyBrandsInThatCategory()
    {
        var categoryA = CategoryId.NewId();
        var categoryB = CategoryId.NewId();

        var brandInA = await new BrandBuilder().WithCategoryId(categoryA).WithName("In A").WithSlug("in-a").BuildAsync();
        var brandInB = await new BrandBuilder().WithCategoryId(categoryB).WithName("In B").WithSlug("in-b").BuildAsync();
        foreach (var b in new[] { brandInA, brandInB })
            b.ClearDomainEvents();

        await _context.Brands.AddRangeAsync(brandInA, brandInB);
        await _context.SaveChangesAsync();

        var result = await _sut.GetPublicBrandsAsync(categoryA);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("In A");
        result[0].CategoryId.ShouldBe(categoryA.Value);
    }
}
