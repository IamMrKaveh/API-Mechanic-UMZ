using Application.Support.Features.Commands.CreateTicket;
using Mapster;
using Presentation.Support.Mapping;
using Presentation.Support.Requests;

namespace Tests.Presentation.Support.Mapping;

public class SupportMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly SupportMappingConfig _sut = new();

    public SupportMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void CreateTicketRequest_MapsToCommand()
    {
        // Arrange
        var request = new CreateTicketRequest("Subject", "Order", "High", "Message");

        // Act
        var command = request.Adapt<CreateTicketCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Subject.ShouldBe(request.Subject);
        command.Category.ShouldBe(request.Category);
        command.Priority.ShouldBe(request.Priority);
        command.Message.ShouldBe(request.Message);
    }

    [Fact]
    public void SupportMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
