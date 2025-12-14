using Infrastructure.Search.Interfaces;

namespace Tests.Integration.Search;

public class ElasticsearchIntegrationTests : IClassFixture<ElasticsearchTestFixture>
{
    private readonly ElasticsearchTestFixture _fixture;
    private readonly ISearchService _searchService;
    private readonly IElasticIndexManager _indexManager;
    private readonly IElasticBulkService _bulkService;

    public ElasticsearchIntegrationTests(ElasticsearchTestFixture fixture)
    {
        _fixture = fixture;
        _searchService = _fixture.ServiceProvider.GetRequiredService<ISearchService>();
        _indexManager = _fixture.ServiceProvider.GetRequiredService<IElasticIndexManager>();
        _bulkService = _fixture.ServiceProvider.GetRequiredService<IElasticBulkService>();
    }

    [Fact]
    public async Task CreateIndices_ShouldSucceed()
    {
        // Act
        var result = await _indexManager.CreateAllIndicesAsync();

        // Assert
        result.Should().BeTrue();

        var productsExist = await _indexManager.IndexExistsAsync("products_v1");
        var categoriesExist = await _indexManager.IndexExistsAsync("categories_v1");
        var categoryGroupsExist = await _indexManager.IndexExistsAsync("categorygroups_v1");

        productsExist.Should().BeTrue();
        categoriesExist.Should().BeTrue();
        categoryGroupsExist.Should().BeTrue();
    }

    [Fact]
    public async Task IndexProduct_ShouldSucceed()
    {
        // Arrange
        var product = new ProductSearchDocument
        {
            Id = 1,
            Name = "تست محصول",
            Description = "توضیحات تست",
            CategoryName = "دسته‌بندی تست",
            CategoryId = 1,
            CategoryGroupName = "گروه تست",
            CategoryGroupId = 1,
            MinPrice = 1000,
            MaxPrice = 2000,
            HasDiscount = true,
            IsInStock = true,
            CreatedAt = DateTime.UtcNow,
            ImageUrl = "https://example.com/image.jpg"
        };

        // Act
        await _searchService.IndexProductAsync(product, CancellationToken.None);

        // Wait for indexing
        await Task.Delay(1000);

        // Assert
        var searchResult = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "تست",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 1,
                PageSize: 10
            ),
            CancellationToken.None
        );

        searchResult.Items.Should().NotBeEmpty();
        searchResult.Total.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SearchProducts_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var products = new List<ProductSearchDocument>
        {
            new()
            {
                Id = 101,
                Name = "لپ‌تاپ ایسوس",
                CategoryName = "لپ‌تاپ",
                CategoryId = 5,
                MinPrice = 20000000,
                IsInStock = true,
                HasDiscount = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 102,
                Name = "لپ‌تاپ دل",
                CategoryName = "لپ‌تاپ",
                CategoryId = 5,
                MinPrice = 30000000,
                IsInStock = true,
                HasDiscount = false,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = 103,
                Name = "موس لاجیتک",
                CategoryName = "جانبی",
                CategoryId = 10,
                MinPrice = 500000,
                IsInStock = false,
                HasDiscount = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        await _bulkService.BulkIndexProductsAsync(products);
        await Task.Delay(2000); // Wait for indexing

        // Act
        var result = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "لپ‌تاپ",
                CategoryId: 5,
                CategoryGroupId: null,
                MinPrice: 15000000,
                MaxPrice: 25000000,
                IsInStock: true,
                HasDiscount: true,
                Page: 1,
                PageSize: 10
            ),
            CancellationToken.None
        );

        // Assert
        result.Items.Should().NotBeEmpty();
        result.Items.Should().OnlyContain(p =>
            p.CategoryId == 5 &&
            p.MinPrice >= 15000000 &&
            p.MinPrice <= 25000000 &&
            p.IsInStock &&
            p.HasDiscount
        );
    }

    [Fact]
    public async Task SearchProducts_WithSorting_ShouldReturnSortedResults()
    {
        // Arrange
        var products = new List<ProductSearchDocument>
        {
            new() { Id = 201, Name = "محصول ارزان", MinPrice = 1000, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = 202, Name = "محصول متوسط", MinPrice = 5000, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new() { Id = 203, Name = "محصول گران", MinPrice = 10000, CreatedAt = DateTime.UtcNow }
        };

        await _bulkService.BulkIndexProductsAsync(products);
        await Task.Delay(2000);

        // Act - Sort by price ascending
        var resultAsc = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "محصول",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 1,
                PageSize: 10,
                Sort: "price_asc"
            ),
            CancellationToken.None
        );

        // Assert
        resultAsc.Items.Should().NotBeEmpty();
        var prices = resultAsc.Items.Select(p => p.MinPrice).ToList();
        prices.Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GlobalSearch_ShouldSearchAcrossAllIndices()
    {
        // Arrange
        var product = new ProductSearchDocument
        {
            Id = 301,
            Name = "گوشی سامسونگ",
            CategoryName = "موبایل",
            CreatedAt = DateTime.UtcNow
        };

        var category = new CategorySearchDocument
        {
            Id = 1,
            Name = "موبایل"
        };

        await _searchService.IndexProductAsync(product, CancellationToken.None);
        await _searchService.IndexCategoryAsync(category, CancellationToken.None);
        await Task.Delay(2000);

        // Act
        var result = await _searchService.SearchGlobalAsync("موبایل", CancellationToken.None);

        // Assert
        result.Products.Should().NotBeEmpty();
        result.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BulkOperations_ShouldHandleLargeDatasets()
    {
        // Arrange
        var products = Enumerable.Range(1, 5000).Select(i => new ProductSearchDocument
        {
            Id = 10000 + i,
            Name = $"محصول تستی {i}",
            CategoryName = "تست",
            CategoryId = 1,
            MinPrice = i * 1000,
            IsInStock = i % 2 == 0,
            HasDiscount = i % 3 == 0,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        // Act
        var result = await _bulkService.BulkIndexProductsAsync(products);

        // Assert
        result.Should().BeTrue();

        await Task.Delay(3000); // Wait for bulk indexing

        var searchResult = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "تستی",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 1,
                PageSize: 100
            ),
            CancellationToken.None
        );

    }

    [Fact]
    public async Task SearchWithHighlighting_ShouldReturnHighlightedResults()
    {
        // Arrange
        var product = new ProductSearchDocument
        {
            Id = 401,
            Name = "کیبورد گیمینگ مکانیکال",
            Description = "کیبورد مکانیکال با کلیدهای چری",
            CategoryName = "جانبی",
            CreatedAt = DateTime.UtcNow
        };

        await _searchService.IndexProductAsync(product, CancellationToken.None);
        await Task.Delay(1000);

        // Act
        var result = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "کیبورد",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 1,
                PageSize: 10
            ),
            CancellationToken.None
        );

        // Assert
        result.Highlights.Should().NotBeEmpty();
        result.Highlights.Values.Should().Contain(h =>
            h.Values.Any(v => v.Any(s => s.Contains("<em>") && s.Contains("</em>")))
        );
    }

    [Fact]
    public async Task Pagination_ShouldWorkCorrectly()
    {
        // Arrange
        var products = Enumerable.Range(1, 50).Select(i => new ProductSearchDocument
        {
            Id = 20000 + i,
            Name = $"محصول صفحه‌بندی {i}",
            CategoryName = "صفحه‌بندی",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _bulkService.BulkIndexProductsAsync(products);
        await Task.Delay(2000);

        // Act - Page 1
        var page1 = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "صفحه‌بندی",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 1,
                PageSize: 20
            ),
            CancellationToken.None
        );

        // Act - Page 2
        var page2 = await _searchService.SearchProductsAsync(
            new SearchProductsQuery(
                Q: "صفحه‌بندی",
                CategoryId: null,
                CategoryGroupId: null,
                MinPrice: null,
                MaxPrice: null,
                IsInStock: null,
                HasDiscount: null,
                Page: 2,
                PageSize: 20
            ),
            CancellationToken.None
        );

        // Assert
        page1.Items.Count().Should().Be(20);
        page2.Items.Count().Should().BeGreaterThan(0);
        page1.Items.Should().NotIntersectWith(page2.Items);
        page1.Total.Should().Be(page2.Total);
    }
}

// Test Fixture
public class ElasticsearchTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public ElasticsearchTestFixture()
    {
        var services = new ServiceCollection();

        // Configure test Elasticsearch connection
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Elasticsearch:Url", "http://localhost:9200" },
                { "Elasticsearch:Index", "test_products_v1" }
            }!)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();

        ServiceProvider = services.BuildServiceProvider();

        // Initialize test indices
        var indexManager = ServiceProvider.GetRequiredService<IElasticIndexManager>();
        indexManager.CreateAllIndicesAsync().Wait();
    }

    public void Dispose()
    {
        // Cleanup test indices
        var indexManager = ServiceProvider.GetRequiredService<IElasticIndexManager>();
        indexManager.DeleteIndexAsync("test_products_v1").Wait();
        indexManager.DeleteIndexAsync("test_categories_v1").Wait();
        indexManager.DeleteIndexAsync("test_categorygroups_v1").Wait();

        (ServiceProvider as IDisposable)?.Dispose();
    }
}