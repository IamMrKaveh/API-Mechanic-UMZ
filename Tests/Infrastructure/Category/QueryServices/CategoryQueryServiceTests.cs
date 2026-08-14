using Application.Common.Contracts;
using Domain.Category.ValueObjects;
using Infrastructure.Category.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Attributes;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;

namespace Tests.Infrastructure.Category.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class CategoryQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private IUrlResolverService _urlResolver = null!; private CategoryQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver.ResolveMediaUrl(Arg.Any<string>()).Returns(ci =>
        {
            var path = ci.Arg<string>();
            return string.IsNullOrWhiteSpace(path) ? string.Empty : $"https://cdn.test/{path}";
        });

        _sut = new CategoryQueryService(_context, _urlResolver);
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
    public async Task GetCategoryDetailAsync_NonExistentCategory_ReturnsNull()
    {
        var result = await _sut.GetCategoryDetailAsync(CategoryId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCategoryDetailAsync_ExistingCategoryWithPrimaryIconAndBrands_ReturnsMappedDetail()
    {
        var category = await new CategoryBuilder()
            .WithName("Detail Category")
            .WithSlug("detail-category")
            .WithDescription("detail description")
            .WithSortOrder(7)
            .BuildAsync();

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var icon = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(category.Id.Value)
            .WithFilePath("uploads/cats/detail/icon.png")
            .WithFileName("icon.png")
            .BuildPrimary();

        _context.Medias.Add(icon);

        var brand1 = await new BrandBuilder()
            .WithName("Detail Brand One")
            .WithSlug("detail-brand-one")
            .WithCategoryId(category.Id)
            .BuildAsync();

        var brand2 = await new BrandBuilder()
            .WithName("Detail Brand Two")
            .WithSlug("detail-brand-two")
            .WithCategoryId(category.Id)
            .BuildAsync();

        _context.Brands.Add(brand1);
        _context.Brands.Add(brand2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryDetailAsync(category.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(category.Id.Value);
        result.Name.ShouldBe("Detail Category");
        result.Slug.ShouldBe("detail-category");
        result.Description.ShouldBe("detail description");
        result.SortOrder.ShouldBe(7);
        result.IsActive.ShouldBeTrue();
        result.BrandCount.ShouldBe(2);
        result.IconUrl.ShouldBe("https://cdn.test/uploads/cats/detail/icon.png");
        result.RowVersion.ShouldNotBeNullOrWhiteSpace();
    }

    [RequiresDockerFact]
    public async Task GetCategoryDetailAsync_ExistingCategoryWithoutPrimaryMedia_ReturnsNullIconUrl()
    {
        var category = await new CategoryBuilder()
            .WithName("No Icon Category")
            .WithSlug("no-icon-category")
            .BuildAsync();

        _context.Categories.Add(category);

        var nonPrimary = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(category.Id.Value)
            .WithIsPrimary(false)
            .Build();

        _context.Medias.Add(nonPrimary);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryDetailAsync(category.Id);

        result.ShouldNotBeNull();
        result.IconUrl.ShouldBeNull();
        result.BrandCount.ShouldBe(0);
    }

    [RequiresDockerFact]
    public async Task GetCategoryTreeAsync_MixedActiveAndInactive_ReturnsOnlyActiveOrderedBySortOrderThenName()
    {
        var active1 = await new CategoryBuilder()
            .WithName("Alpha Tree")
            .WithSlug("alpha-tree")
            .WithSortOrder(2)
            .BuildAsync();

        var active2 = await new CategoryBuilder()
            .WithName("Beta Tree")
            .WithSlug("beta-tree")
            .WithSortOrder(1)
            .BuildAsync();

        var active3 = await new CategoryBuilder()
            .WithName("Charlie Tree")
            .WithSlug("charlie-tree")
            .WithSortOrder(2)
            .BuildAsync();

        var inactive = await new CategoryBuilder()
            .WithName("Delta Tree")
            .WithSlug("delta-tree")
            .WithSortOrder(0)
            .BuildAsync();
        inactive.Deactivate();

        _context.Categories.AddRange(active1, active2, active3, inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryTreeAsync();

        result.Count.ShouldBe(3);
        result.ShouldAllBe(c => c.IsActive);
        result[0].Name.ShouldBe("Beta Tree");
        result[1].Name.ShouldBe("Alpha Tree");
        result[2].Name.ShouldBe("Charlie Tree");
    }

    [RequiresDockerFact]
    public async Task GetCategoriesPagedAsync_SearchTermCaseInsensitive_ReturnsMatchingCategoriesOnly()
    {
        var a = await new CategoryBuilder().WithName("Engine Parts").WithSlug("engine-parts-p1").BuildAsync();
        var b = await new CategoryBuilder().WithName("Body Kits").WithSlug("body-kits-p1").BuildAsync();
        var c = await new CategoryBuilder().WithName("Electronic Sensors").WithSlug("electronic-sensors-p1").BuildAsync();

        _context.Categories.AddRange(a, b, c);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoriesPagedAsync("ENGINE", isActive: null, includeDeleted: false, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Engine Parts");
    }

    [RequiresDockerFact]
    public async Task GetCategoriesPagedAsync_IsActiveFilter_ReturnsOnlyMatchingActiveState()
    {
        var active = await new CategoryBuilder().WithName("Active Paged").WithSlug("active-paged").BuildAsync();
        var inactive = await new CategoryBuilder().WithName("Inactive Paged").WithSlug("inactive-paged").BuildAsync();
        inactive.Deactivate();

        _context.Categories.AddRange(active, inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var activeResult = await _sut.GetCategoriesPagedAsync(search: null, isActive: true, includeDeleted: false, page: 1, pageSize: 10);
        var inactiveResult = await _sut.GetCategoriesPagedAsync(search: null, isActive: false, includeDeleted: false, page: 1, pageSize: 10);

        activeResult.TotalCount.ShouldBe(1);
        activeResult.Items[0].Name.ShouldBe("Active Paged");
        inactiveResult.TotalCount.ShouldBe(1);
        inactiveResult.Items[0].Name.ShouldBe("Inactive Paged");
    }

    [RequiresDockerFact]
    public async Task GetCategoriesPagedAsync_WithPagination_ReturnsRequestedPage()
    {
        for (var i = 0; i < 5; i++)
        {
            var cat = await new CategoryBuilder()
                .WithName($"Pager Cat {i:D2}")
                .WithSlug($"pager-cat-{i:D2}")
                .WithSortOrder(i)
                .BuildAsync();
            _context.Categories.Add(cat);
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var page1 = await _sut.GetCategoriesPagedAsync(search: null, isActive: null, includeDeleted: false, page: 1, pageSize: 2);
        var page2 = await _sut.GetCategoriesPagedAsync(search: null, isActive: null, includeDeleted: false, page: 2, pageSize: 2);
        var page3 = await _sut.GetCategoriesPagedAsync(search: null, isActive: null, includeDeleted: false, page: 3, pageSize: 2);

        page1.TotalCount.ShouldBe(5);
        page1.Items.Count.ShouldBe(2);
        page2.Items.Count.ShouldBe(2);
        page3.Items.Count.ShouldBe(1);

        var allNames = page1.Items.Select(i => i.Name)
            .Concat(page2.Items.Select(i => i.Name))
            .Concat(page3.Items.Select(i => i.Name))
            .ToList();
        allNames.Distinct().Count().ShouldBe(5);
    }

    [RequiresDockerFact]
    public async Task GetCategoriesPagedAsync_WithPrimaryActiveIcon_ResolvesIconUrl()
    {
        var category = await new CategoryBuilder()
            .WithName("Iconed Paged")
            .WithSlug("iconed-paged")
            .BuildAsync();

        _context.Categories.Add(category);

        var icon = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(category.Id.Value)
            .WithFilePath("uploads/cats/iconed/icon.png")
            .WithFileName("icon.png")
            .BuildPrimary();

        _context.Medias.Add(icon);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoriesPagedAsync(search: null, isActive: null, includeDeleted: false, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IconUrl.ShouldBe("https://cdn.test/uploads/cats/iconed/icon.png");
        result.Items[0].ProductCount.ShouldBe(0);
    }

    [RequiresDockerFact]
    public async Task GetCategoryWithBrandsAsync_NonExistentCategory_ReturnsNull()
    {
        var result = await _sut.GetCategoryWithBrandsAsync(CategoryId.NewId());

        result.ShouldBeNull();
    }

    [RequiresDockerFact]
    public async Task GetCategoryWithBrandsAsync_ExcludesDeletedBrandsAndResolvesLogo()
    {
        var category = await new CategoryBuilder()
            .WithName("With Brands Cat")
            .WithSlug("with-brands-cat")
            .BuildAsync();

        _context.Categories.Add(category);

        var liveBrand = await new BrandBuilder()
            .WithName("Live Brand")
            .WithSlug("live-brand-wb")
            .WithCategoryId(category.Id)
            .WithLogoPath("uploads/brands/live/logo.png")
            .BuildAsync();

        var deletedBrand = await new BrandBuilder()
            .WithName("Deleted Brand")
            .WithSlug("deleted-brand-wb")
            .WithCategoryId(category.Id)
            .BuildAsync();
        deletedBrand.RequestDeletion();

        _context.Brands.Add(liveBrand);
        _context.Brands.Add(deletedBrand);

        var icon = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(category.Id.Value)
            .WithFilePath("uploads/cats/wb/icon.png")
            .WithFileName("icon.png")
            .BuildPrimary();
        _context.Medias.Add(icon);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryWithBrandsAsync(category.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(category.Id.Value);
        result.Name.ShouldBe("With Brands Cat");
        result.IconUrl.ShouldBe("https://cdn.test/uploads/cats/wb/icon.png");
        result.Brands.Count.ShouldBe(1);
        result.Brands[0].Name.ShouldBe("Live Brand");
        result.Brands[0].LogoPath.ShouldBe("https://cdn.test/uploads/brands/live/logo.png");
    }

    [RequiresDockerFact]
    public async Task GetCategoryProductsAsync_FiltersByCategoryAndOrdersByCreatedAtDescending()
    {
        var targetCategory = await new CategoryBuilder()
            .WithName("Target Products Cat")
            .WithSlug("target-products-cat")
            .BuildAsync();

        var otherCategory = await new CategoryBuilder()
            .WithName("Other Products Cat")
            .WithSlug("other-products-cat")
            .BuildAsync();

        _context.Categories.Add(targetCategory);
        _context.Categories.Add(otherCategory);

        var targetBrand = await new BrandBuilder()
            .WithName("Target Brand P")
            .WithSlug("target-brand-p")
            .WithCategoryId(targetCategory.Id)
            .BuildAsync();

        var otherBrand = await new BrandBuilder()
            .WithName("Other Brand P")
            .WithSlug("other-brand-p")
            .WithCategoryId(otherCategory.Id)
            .BuildAsync();

        _context.Brands.Add(targetBrand);
        _context.Brands.Add(otherBrand);

        var productA = new ProductBuilder()
            .WithName("Product A")
            .WithSlug("product-a-tpc")
            .WithBrandId(targetBrand.Id)
            .WithCategoryId(targetCategory.Id)
            .Build();

        await Task.Delay(5);

        var productB = new ProductBuilder()
            .WithName("Product B")
            .WithSlug("product-b-tpc")
            .WithBrandId(targetBrand.Id)
            .WithCategoryId(targetCategory.Id)
            .Build();

        var productOther = new ProductBuilder()
            .WithName("Product Other")
            .WithSlug("product-other-tpc")
            .WithBrandId(otherBrand.Id)
            .WithCategoryId(otherCategory.Id)
            .Build();

        _context.Products.AddRange(productA, productB, productOther);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryProductsAsync(targetCategory.Id, activeOnly: false, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items[0].Name.ShouldBe("Product B");
        result.Items[1].Name.ShouldBe("Product A");
        result.Items.ShouldAllBe(p => p.BrandName == "Target Brand P");
        result.Items.ShouldAllBe(p => p.MinPrice == 0m && p.MaxPrice == 0m);
    }

    [RequiresDockerFact]
    public async Task GetCategoryProductsAsync_ActiveOnlyTrue_ExcludesInactiveProducts()
    {
        var category = await new CategoryBuilder()
            .WithName("Active Products Cat")
            .WithSlug("active-products-cat")
            .BuildAsync();

        _context.Categories.Add(category);

        var brand = await new BrandBuilder()
            .WithName("Active Filter Brand")
            .WithSlug("active-filter-brand")
            .WithCategoryId(category.Id)
            .BuildAsync();

        _context.Brands.Add(brand);

        var activeProduct = new ProductBuilder()
            .WithName("Active Product")
            .WithSlug("active-product-apc")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        var inactiveProduct = new ProductBuilder()
            .WithName("Inactive Product")
            .WithSlug("inactive-product-apc")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        inactiveProduct.Deactivate();

        _context.Products.AddRange(activeProduct, inactiveProduct);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetCategoryProductsAsync(category.Id, activeOnly: true, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("Active Product");
        result.Items[0].IsActive.ShouldBeTrue();
    }

    [RequiresDockerFact]
    public async Task GetPublicCategoriesAsync_ReturnsOnlyActiveCategoriesFilteredBySearch()
    {
        var active1 = await new CategoryBuilder().WithName("Public Alpha").WithSlug("public-alpha").WithSortOrder(2).BuildAsync();
        var active2 = await new CategoryBuilder().WithName("Public Bravo").WithSlug("public-bravo").WithSortOrder(1).BuildAsync();
        var inactive = await new CategoryBuilder().WithName("Public Charlie").WithSlug("public-charlie").WithSortOrder(0).BuildAsync();
        inactive.Deactivate();

        _context.Categories.AddRange(active1, active2, inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var all = await _sut.GetPublicCategoriesAsync(search: null, page: 1, pageSize: 10);
        var searched = await _sut.GetPublicCategoriesAsync(search: "alpha", page: 1, pageSize: 10);

        all.TotalCount.ShouldBe(2);
        all.Items.Select(i => i.Name).ShouldNotContain("Public Charlie");
        all.Items[0].Name.ShouldBe("Public Bravo");
        all.Items[1].Name.ShouldBe("Public Alpha");

        searched.TotalCount.ShouldBe(1);
        searched.Items[0].Name.ShouldBe("Public Alpha");
    }

    [RequiresDockerFact]
    public async Task GetPublicCategoriesAsync_ResolvesIconUrlFromPrimaryActiveMedia()
    {
        var category = await new CategoryBuilder()
            .WithName("Public Iconed")
            .WithSlug("public-iconed")
            .BuildAsync();

        _context.Categories.Add(category);

        var icon = new MediaBuilder()
            .WithEntityType("Category")
            .WithEntityId(category.Id.Value)
            .WithFilePath("uploads/cats/public/icon.png")
            .WithFileName("icon.png")
            .BuildPrimary();
        _context.Medias.Add(icon);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPublicCategoriesAsync(search: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IconUrl.ShouldBe("https://cdn.test/uploads/cats/public/icon.png");
    }
}
