using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Shared;

namespace Tests.Application.Product.Features.Commands.BulkUpdatePrices;

public class BulkUpdatePricesValidatorTests
{
    private readonly BulkUpdatePricesValidator _sut = new();

    private static VariantPriceUpdateInput ValidUpdate(
        Guid? productId = null,
        Guid? variantId = null,
        decimal sellingPrice = 100_000m,
        decimal originalPrice = 120_000m)
        => new(
            productId ?? Guid.NewGuid(),
            variantId ?? Guid.NewGuid(),
            sellingPrice,
            originalPrice);

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput> { ValidUpdate() });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUpdates_FailsOnUpdates()
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>());

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(BulkUpdatePricesCommand.Updates));
    }

    [Fact]
    public void Validate_WithEmptyProductIdInItem_Fails()
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
    {
        ValidUpdate(productId: Guid.Empty)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.EndsWith(nameof(VariantPriceUpdateInput.ProductId)));
    }

    [Fact]
    public void Validate_WithEmptyVariantIdInItem_Fails()
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
    {
        ValidUpdate(variantId: Guid.Empty)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.EndsWith(nameof(VariantPriceUpdateInput.VariantId)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveSellingPriceInItem_Fails(decimal sellingPrice)
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
    {
        ValidUpdate(sellingPrice: sellingPrice)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.EndsWith(nameof(VariantPriceUpdateInput.SellingPrice)));
    }

    [Fact]
    public void Validate_WithNegativeOriginalPriceInItem_Fails()
    {
        var command = new BulkUpdatePricesCommand(new List<VariantPriceUpdateInput>
    {
        ValidUpdate(originalPrice: -1m)
    });

        var result = _sut.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName.EndsWith(nameof(VariantPriceUpdateInput.OriginalPrice)));
    }
}
