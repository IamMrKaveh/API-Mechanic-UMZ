using Application.Payment.Features.Commands.CreatePaymentMethod;
using Application.Payment.Features.Commands.UpdatePaymentMethod;
using Mapster;
using Presentation.Payment.Mapping;
using Presentation.Payment.Requests;

namespace Tests.Presentation.Payment.Mapping;

public class PaymentMethodMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly PaymentMethodMappingConfig _sut = new();

    public PaymentMethodMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void CreatePaymentMethodRequest_MapsToCommand()
    {
        // Arrange
        var request = new CreatePaymentMethodRequest("ZarinPal", "zarinpal", "Desc", "icon.png", 1000, 2.5m, 1);

        // Act
        var command = request.Adapt<CreatePaymentMethodCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Name.ShouldBe(request.Name);
        command.Code.ShouldBe(request.Code);
        command.Description.ShouldBe(request.Description);
        command.IconUrl.ShouldBe(request.IconUrl);
        command.FeeAmount.ShouldBe(request.FeeAmount);
        command.FeePercentage.ShouldBe(request.FeePercentage);
        command.SortOrder.ShouldBe(request.SortOrder);
    }

    [Fact]
    public void CreatePaymentMethodRequest_WithDefaults_MapsToCommand()
    {
        // Arrange
        var request = new CreatePaymentMethodRequest("COD", "cod");

        // Act
        var command = request.Adapt<CreatePaymentMethodCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Name.ShouldBe("COD");
        command.Code.ShouldBe("cod");
        command.Description.ShouldBeNull();
        command.FeeAmount.ShouldBe(0m);
        command.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void UpdatePaymentMethodRequest_MapsToCommand_ExceptId()
    {
        // Arrange
        var request = new UpdatePaymentMethodRequest("ZarinPal", "Desc", "icon.png", 1000, 2.5m, 3);

        // Act
        var command = request.Adapt<UpdatePaymentMethodCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.Id.ShouldBe(Guid.Empty);
        command.Name.ShouldBe(request.Name);
        command.Description.ShouldBe(request.Description);
        command.IconUrl.ShouldBe(request.IconUrl);
        command.FeeAmount.ShouldBe(request.FeeAmount);
        command.FeePercentage.ShouldBe(request.FeePercentage);
        command.SortOrder.ShouldBe(request.SortOrder);
    }

    [Fact]
    public void PaymentMethodMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
