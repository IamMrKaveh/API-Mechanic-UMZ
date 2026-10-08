using Application.User.Features.Commands.UpdateProfile;
using Mapster;
using Presentation.User.Mapping;
using Presentation.User.Requests;

namespace Tests.Presentation.User.Mapping;

public class UserMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly UserMappingConfig _sut = new();

    public UserMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void UpdateProfileRequest_MapsToCommand()
    {
        // Arrange
        var request = new UpdateProfileRequest("Ali", "Rezaei");

        // Act
        var command = request.Adapt<UpdateProfileCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.FirstName.ShouldBe(request.FirstName);
        command.LastName.ShouldBe(request.LastName);
    }

    [Fact]
    public void UserMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
