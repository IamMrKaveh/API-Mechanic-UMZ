using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetPublicBrands;
using Mapster;
using Presentation.Brand.Mapping;
using Presentation.Brand.Requests;

namespace Tests.Presentation.Brand.Mapping;

public class BrandMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly BrandMappingConfig _sut = new();

    public BrandMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void MoveBrandRequest_MapsToCommand()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var targetCategoryId = Guid.NewGuid();
        var request = new MoveBrandRequest(brandId, targetCategoryId);

        // Act
        var command = request.Adapt<MoveBrandCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.BrandId.ShouldBe(request.BrandId);
        command.TargetCategoryId.ShouldBe(request.TargetCategoryId);
    }

    [Fact]
    public void GetAdminBrandsRequest_MapsToQuery()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new GetAdminBrandsRequest
        {
            CategoryId = categoryId,
            Search = "brand",
            IsActive = true,
            IncludeDeleted = true,
            Page = 2,
            PageSize = 25
        };

        // Act
        var query = request.Adapt<GetAdminBrandsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBe(request.CategoryId);
        query.Search.ShouldBe(request.Search);
        query.IsActive.ShouldBe(request.IsActive);
        query.IncludeDeleted.ShouldBe(request.IncludeDeleted);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
    }

    [Fact]
    public void GetAdminBrandsRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetAdminBrandsRequest();

        // Act
        var query = request.Adapt<GetAdminBrandsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(10);
        query.IncludeDeleted.ShouldBeFalse();
    }

    [Fact]
    public void GetPublicBrandsRequest_MapsToQuery()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new GetPublicBrandsRequest(categoryId);

        // Act
        var query = request.Adapt<GetPublicBrandsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBe(request.CategoryId);
    }

    [Fact]
    public void GetPublicBrandsRequest_WithNullCategory_MapsToQuery()
    {
        // Arrange
        var request = new GetPublicBrandsRequest(null);

        // Act
        var query = request.Adapt<GetPublicBrandsQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.CategoryId.ShouldBeNull();
    }

    [Fact]
    public void BrandMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
