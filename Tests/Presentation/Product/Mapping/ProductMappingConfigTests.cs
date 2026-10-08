using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProducts;
using Mapster;
using Presentation.Product.Mapping;
using Presentation.Product.Requests;

namespace Tests.Presentation.Product.Mapping;

public class ProductMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly ProductMappingConfig _sut = new();

    public ProductMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetProductsRequest_MapsToQuery()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var request = new GetProductsRequest(2, 25, "phone", categoryId, brandId, 100, 1000, true, "Price");

        // Act
        var query = request.Adapt<GetProductsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
        query.Search.ShouldBe(request.Search);
        query.CategoryId.ShouldBe(request.CategoryId);
        query.BrandId.ShouldBe(request.BrandId);
    }

    [Fact]
    public void GetProductsRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetProductsRequest();

        // Act
        var query = request.Adapt<GetProductsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(10);
        query.Search.ShouldBeNull();
        query.CategoryId.ShouldBeNull();
        query.BrandId.ShouldBeNull();
    }

    [Fact]
    public void GetProductCatalogRequest_MapsToQuery()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new GetProductCatalogRequest(2, 25, "phone", categoryId, null, 100, 1000, true, "Price", true);

        // Act
        var query = request.Adapt<GetProductCatalogQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
        query.Search.ShouldBe(request.Search);
        query.CategoryId.ShouldBe(request.CategoryId);
        query.MinPrice.ShouldBe(request.MinPrice);
        query.MaxPrice.ShouldBe(request.MaxPrice);
        query.InStockOnly.ShouldBe(request.InStockOnly);
        query.SortBy.ShouldBe(request.SortBy);
        query.IsFeatured.ShouldBe(request.IsFeatured);
    }

    [Fact]
    public void ProductMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
