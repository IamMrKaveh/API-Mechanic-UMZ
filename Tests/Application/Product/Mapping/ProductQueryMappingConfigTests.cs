using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Shared;
using Application.Product.Mapping;
using Mapster;

namespace Tests.Application.Product.Mapping;

public class ProductQueryMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public ProductQueryMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new ProductQueryMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_SearchParams_ToCatalogQuery_MapsPagingFiltersAndSort()
    {
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var search = new ProductCatalogSearchParams(
            3, 25, "brake", categoryId, brandId, 100m, 500m, true, "price_asc", true, false);

        var query = _mapper.Map<GetProductCatalogQuery>(search);

        query.Page.ShouldBe(3);
        query.PageSize.ShouldBe(25);
        query.Search.ShouldBe("brake");
        query.CategoryId.ShouldBe(categoryId);
        query.BrandId.ShouldBe(brandId);
        query.MinPrice.ShouldBe(100m);
        query.MaxPrice.ShouldBe(500m);
        query.InStockOnly.ShouldBeTrue();
        query.SortBy.ShouldBe("price_asc");
    }

    [Fact]
    public void Map_SearchParams_WithNulls_PreservesNulls()
    {
        var search = new ProductCatalogSearchParams(1, 10, null, null, null, null, null, false, null, null);

        var query = _mapper.Map<GetProductCatalogQuery>(search);

        query.Search.ShouldBeNull();
        query.CategoryId.ShouldBeNull();
        query.SortBy.ShouldBeNull();
        query.IsFeatured.ShouldBeNull();
        query.HasDiscount.ShouldBeNull();
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new ProductQueryMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
