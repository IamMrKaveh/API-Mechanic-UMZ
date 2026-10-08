using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Mapster;
using Presentation.Shipping.Mapping;
using Presentation.Shipping.Requests;

namespace Tests.Presentation.Shipping.Mapping;

public class ShippingMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly ShippingMappingConfig _sut = new();

    public ShippingMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void CreateShippingRequest_MapsToCommand()
    {
        // Arrange
        var request = new CreateShippingRequest("Post", 50000, "Desc", "2-3 days", 1, 3);

        // Act
        var command = request.Adapt<CreateShippingCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Name.ShouldBe(request.Name);
        command.BaseCost.ShouldBe(request.BaseCost);
        command.Description.ShouldBe(request.Description);
        command.EstimatedDeliveryTime.ShouldBe(request.EstimatedDeliveryTime);
        command.MinDeliveryDays.ShouldBe(request.MinDeliveryDays);
        command.MaxDeliveryDays.ShouldBe(request.MaxDeliveryDays);
    }

    [Fact]
    public void CreateShippingRequest_WithDefaults_MapsToCommand()
    {
        // Arrange
        var request = new CreateShippingRequest("Post", 50000);

        // Act
        var command = request.Adapt<CreateShippingCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Description.ShouldBeNull();
        command.MinDeliveryDays.ShouldBe(1);
        command.MaxDeliveryDays.ShouldBe(7);
    }

    [Fact]
    public void UpdateShippingRequest_MapsToCommand_ExceptId()
    {
        // Arrange
        var request = new UpdateShippingRequest("Post", 60000, "Desc", "3-4 days", 2, 4);

        // Act
        var command = request.Adapt<UpdateShippingCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Id.ShouldBe(Guid.Empty);
        command.Name.ShouldBe(request.Name);
        command.BaseCost.ShouldBe(request.BaseCost);
        command.MinDeliveryDays.ShouldBe(request.MinDeliveryDays);
        command.MaxDeliveryDays.ShouldBe(request.MaxDeliveryDays);
    }

    [Fact]
    public void ShippingMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
