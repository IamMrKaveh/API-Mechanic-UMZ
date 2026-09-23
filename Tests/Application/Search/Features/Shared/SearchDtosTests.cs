using Application.Search.Features.Shared;

namespace Tests.Application.Search.Features.Shared;

public class SearchDtosTests
{
    [Fact]
    public void SearchProductsParams_Defaults_AreFirstPage()
    {
        var p = new SearchProductsParams();

        p.Q.ShouldBe(string.Empty);
        p.Page.ShouldBe(1);
        p.PageSize.ShouldBe(20);
        p.CategoryId.ShouldBeNull();
        p.SortBy.ShouldBeNull();
    }

    [Fact]
    public void SearchResultDto_TotalPages_ComputedCorrectly()
    {
        new SearchResultDto<string> { Total = 95, PageSize = 20 }.TotalPages.ShouldBe(5);
        new SearchResultDto<string> { Total = 100, PageSize = 20 }.TotalPages.ShouldBe(5);
        new SearchResultDto<string> { Total = 0, PageSize = 20 }.TotalPages.ShouldBe(0);
        new SearchResultDto<string> { Total = 10, PageSize = 0 }.TotalPages.ShouldBe(0);
    }

    [Fact]
    public void SearchResultDto_PagingFlags_Work()
    {
        var first = new SearchResultDto<string> { Total = 50, Page = 1, PageSize = 20 };
        first.HasPreviousPage.ShouldBeFalse();
        first.HasNextPage.ShouldBeTrue();

        var last = new SearchResultDto<string> { Total = 50, Page = 3, PageSize = 20 };
        last.HasPreviousPage.ShouldBeTrue();
        last.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public void ProductSearchResultItemDto_Defaults_AreEmpty()
    {
        var dto = new ProductSearchResultItemDto();

        dto.Name.ShouldBe(string.Empty);
        dto.Images.ShouldNotBeNull();
        dto.Images.ShouldBeEmpty();
    }

    [Fact]
    public void GlobalSearchResultDto_Defaults_AreEmpty()
    {
        var dto = new GlobalSearchResultDto();

        dto.Products.ShouldBeEmpty();
        dto.Categories.ShouldBeEmpty();
        dto.Brands.ShouldBeEmpty();
        dto.Query.ShouldBe(string.Empty);
    }

    [Fact]
    public void CategorySearchSummaryDto_RoundTrip()
    {
        var dto = new CategorySearchSummaryDto
        {
            Id = Guid.NewGuid(), Name = "Brakes", Slug = "brakes",
            IsActive = true, ProductCount = 12
        };

        dto.ProductCount.ShouldBe(12);
    }

    [Fact]
    public void BrandSearchSummaryDto_RoundTrip()
    {
        var dto = new BrandSearchSummaryDto
        {
            Id = Guid.NewGuid(), Name = "Bosch", CategoryName = "Tools",
            CategoryId = Guid.NewGuid(), IsActive = true, ProductCount = 30
        };

        dto.CategoryName.ShouldBe("Tools");
    }

    [Fact]
    public void ProductSearchDocument_DefaultCollections_AreEmpty()
    {
        var dto = new ProductSearchDocument();

        dto.Images.ShouldBeEmpty();
        dto.Tags.ShouldBeEmpty();
        dto.Name.ShouldBe(string.Empty);
    }

    [Fact]
    public void CategorySearchDocument_RoundTrip()
    {
        var dto = new CategorySearchDocument
        {
            CategoryId = Guid.NewGuid(), Name = "Engine",
            Slug = "engine", IsActive = true, ProductCount = 5
        };

        dto.Slug.ShouldBe("engine");
    }

    [Fact]
    public void BrandSearchDocument_RoundTrip()
    {
        var dto = new BrandSearchDocument
        {
            BrandId = Guid.NewGuid(), Name = "Siemens",
            CategoryId = Guid.NewGuid(), IsActive = true
        };

        dto.Name.ShouldBe("Siemens");
    }

    [Fact]
    public void FailedElasticOperation_DefaultStatus_IsPending()
    {
        var op = new FailedElasticOperation();

        op.Status.ShouldBe("Pending");
        op.RetryCount.ShouldBe(0);
        op.ProcessedAt.ShouldBeNull();
        op.Id.ShouldBe(default(Guid));
    }
}
