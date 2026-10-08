using Application.Product.Features.Queries.GetAdminProducts;
using Mapster;
using Presentation.Product.Mapping;
using Presentation.Product.Requests;

namespace Tests.Presentation.Product.Mapping;

public class AdminProductMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly AdminProductMappingConfig _sut = new();

    public AdminProductMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetAdminProductsRequest_MapsToQuery()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var request = new GetAdminProductsRequest(2, 25, "phone", categoryId, brandId, true, true);

        // Act
        var query = request.Adapt<GetAdminProductsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBe(request.CategoryId);
        query.BrandId.ShouldBe(request.BrandId);
        query.Search.ShouldBe(request.Search);
        query.IsActive.ShouldBe(request.IsActive);
        query.IncludeDeleted.ShouldBe(request.IncludeDeleted);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
    }

    [Fact]
    public void GetAdminProductsRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetAdminProductsRequest();

        // Act
        var query = request.Adapt<GetAdminProductsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBeNull();
        query.BrandId.ShouldBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(10);
        query.IncludeDeleted.ShouldBeFalse();
    }

    [Fact]
    public void AdminProductMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
