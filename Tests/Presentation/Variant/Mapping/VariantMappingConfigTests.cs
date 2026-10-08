using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Mapster;
using Presentation.Variant.Mapping;
using Presentation.Variant.Requests;

namespace Tests.Presentation.Variant.Mapping;

public class VariantMappingConfigTests
{
    private readonly TypeAdapterConfig _config = new();
    private readonly VariantMappingConfig _sut = new();

    public VariantMappingConfigTests()
    {
        _sut.Register(_config);
        _config.Compile();
    }

    [Fact]
    public void AddVariantRequest_MapsToCommand_IgnoringProductId()
    {
        // Arrange
        var attributeId = Guid.NewGuid();
        var request = new AddVariantRequest("SKU1", 100, 120, 10, false, 1, [attributeId], null);

        // Act
        var command = request.Adapt<AddVariantCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ProductId.ShouldBe(Guid.Empty);
        command.Sku.ShouldBe(request.Sku);
        command.SellingPrice.ShouldBe(request.SellingPrice);
        command.OriginalPrice.ShouldBe(request.OriginalPrice);
        command.Stock.ShouldBe(request.Stock);
        command.IsUnlimited.ShouldBe(request.IsUnlimited);
        command.ShippingMultiplier.ShouldBe(request.ShippingMultiplier);
        command.AttributeValueIds.ShouldContain(attributeId);
    }

    [Fact]
    public void AddVariantRequest_WithNullAttributeIds_MapsToEmptyList()
    {
        // Arrange
        var request = new AddVariantRequest("SKU1", 100, 120);

        // Act
        var command = request.Adapt<AddVariantCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.AttributeValueIds.ShouldNotBeNull();
        command.AttributeValueIds.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateVariantRequest_MapsToCommand()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new UpdateVariantRequest(productId, variantId, userId, "SKU1", 100, 120, 10, false, 1, null, null);

        // Act
        var command = request.Adapt<UpdateVariantCommand>(_config);

        // Assert
        command.ShouldNotBeNull();
        command.ProductId.ShouldBe(request.ProductId);
        command.VariantId.ShouldBe(request.VariantId);
        command.Sku.ShouldBe(request.Sku);
        command.SellingPrice.ShouldBe(request.SellingPrice);
        command.Stock.ShouldBe(request.Stock);
    }

    [Fact]
    public void VariantMappingConfig_ImplementsIRegister()
    {
        _sut.ShouldBeAssignableTo<IRegister>();
    }
}
