using Application.Media.Features.Queries.GetAllMedia;
using Mapster;
using Presentation.Media.Mapping;
using Presentation.Media.Requests;

namespace Tests.Presentation.Media.Mapping;

public class MediaMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly MediaMappingConfig _sut = new();

    public MediaMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void GetAllMediaRequest_MapsToQuery()
    {
        // Arrange
        var request = new GetAllMediaRequest("Product", 2, 25);

        // Act
        var query = request.Adapt<GetAllMediaQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.EntityType.ShouldBe(request.EntityType);
        query.Page.ShouldBe(request.Page);
        query.PageSize.ShouldBe(request.PageSize);
    }

    [Fact]
    public void GetAllMediaRequest_WithDefaults_MapsToQuery()
    {
        // Arrange
        var request = new GetAllMediaRequest();

        // Act
        var query = request.Adapt<GetAllMediaQuery>(_config);

        // Assert
        query.ShouldNotBeNull();
        query.EntityType.ShouldBeNull();
        query.Page.ShouldBe(1);
        query.PageSize.ShouldBe(20);
    }

    [Fact]
    public void MediaMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
