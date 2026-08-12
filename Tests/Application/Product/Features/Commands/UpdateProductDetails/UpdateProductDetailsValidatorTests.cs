using Application.Product.Features.Commands.UpdateProductDetails;

namespace Tests.Application.Product.Features.Commands.UpdateProductDetails;

public class UpdateProductDetailsValidatorTests
{
    private readonly UpdateProductDetailsValidator _sut = new();

    private static UpdateProductDetailsCommand ValidCommand(
        Guid? productId = null,
        string name = "Sample Product",
        string? description = null,
        Guid? brandId = null,
        bool isActive = true,
        string? sku = null,
        string rowVersion = "AAAAAAAAB9E=")
        => new(
            productId ?? Guid.NewGuid(),
            name,
            description,
            brandId ?? Guid.NewGuid(),
            isActive,
            sku,
            rowVersion);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _sut.Validate(ValidCommand());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyProductId_FailsOnProductId()
    {
        var result = _sut.Validate(ValidCommand(productId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.ProductId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_FailsOnName(string name)
    {
        var result = _sut.Validate(ValidCommand(name: name));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.Name));
    }

    [Fact]
    public void Validate_WithNameLongerThanTwoHundredCharacters_FailsOnName()
    {
        var result = _sut.Validate(ValidCommand(name: new string('a', 201)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.Name));
    }

    [Fact]
    public void Validate_WithEmptyBrandId_FailsOnBrandId()
    {
        var result = _sut.Validate(ValidCommand(brandId: Guid.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.BrandId));
    }

    [Fact]
    public void Validate_WithEmptyRowVersion_FailsOnRowVersion()
    {
        var result = _sut.Validate(ValidCommand(rowVersion: string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.RowVersion));
    }

    [Fact]
    public void Validate_WithSkuLongerThanFiftyCharacters_FailsOnSku()
    {
        var result = _sut.Validate(ValidCommand(sku: new string('X', 51)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.Sku));
    }

    [Fact]
    public void Validate_WithNullSku_DoesNotFailOnSku()
    {
        var result = _sut.Validate(ValidCommand(sku: null));

        result.Errors.ShouldNotContain(e => e.PropertyName == nameof(UpdateProductDetailsCommand.Sku));
    }
}
