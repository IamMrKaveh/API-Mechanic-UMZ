using Application.Attribute.Features.Shared;
using Application.Variant.Features.Shared;
using Domain.Variant.ValueObjects;
using AttributeValueEntity = Domain.Attribute.Entities.AttributeValue;
using AttributeValueIdAlias = Domain.Attribute.ValueObjects.AttributeValueId;

namespace Tests.Application.Variant.Features.Shared;

public class VariantDtosTests
{

    [Fact]
    public void ProductVariantViewDto_Defaults_AreEmpty()
    {
        var dto = new ProductVariantViewDto();

        dto.Id.ShouldBe(default(Guid));
        dto.Sku.ShouldBe(string.Empty);
        dto.ShippingMultiplier.ShouldBe(1m);
        dto.EnabledShippingIds.ShouldNotBeNull();
        dto.EnabledShippingIds.ShouldBeEmpty();
        dto.Attributes.ShouldNotBeNull();
        dto.Attributes.ShouldBeEmpty();
    }

    [Fact]
    public void VariantShippingInfoDto_DefaultCollections_AreEmpty()
    {
        var dto = new VariantShippingInfoDto();

        dto.AvailableShippings.ShouldBeEmpty();
        dto.EnabledShippingIds.ShouldBeEmpty();
        dto.ShippingMultiplier.ShouldBe(0m);
    }

    [Fact]
    public void Factory_Create_MapsPricesDiscountAndInventory()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(800m)
            .WithOriginalPrice(1000m)
            .Build();
        var inventory = new InventoryBuilder().WithVariantId(variant.Id).WithInitialStock(12).Build();

        var dto = ProductVariantViewDtoFactory.Create(variant, inventory, new List<AttributeValueEntity>());

        dto.Id.ShouldBe(variant.Id.Value);
        dto.ProductId.ShouldBe(variant.ProductId.Value);
        dto.Sku.ShouldBe(variant.Sku.Value);
        dto.SellingPrice.ShouldBe(800m);
        dto.OriginalPrice.ShouldBe(1000m);
        dto.IsActive.ShouldBe(variant.IsActive);
        dto.HasDiscount.ShouldBeTrue();
        dto.DiscountPercentage.ShouldBe(variant.DiscountPercentage ?? 0m);
        dto.Stock.ShouldBe(12);
        dto.StockQuantity.ShouldBe(12);
        dto.IsUnlimited.ShouldBeFalse();
        dto.IsInStock.ShouldBeTrue();
        dto.Attributes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Factory_Create_WithAttributeValues_ResolvesNamesAndValues()
    {
        var attributeType = await new AttributeTypeBuilder().WithName("color").WithDisplayName("Color").BuildAsync();
        var value = attributeType.AddValue("red", "Red", DateTime.UtcNow, "#FF0000", 1);
        var variant = new ProductVariantBuilder().Build();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(attributeType.Id, value.Id, "Red")
        });
        var inventory = new InventoryBuilder().WithVariantId(variant.Id).Build();

        var dto = ProductVariantViewDtoFactory.Create(
            variant, inventory, new List<AttributeValueEntity> { value });

        dto.Attributes.Count.ShouldBe(1);
        dto.Attributes["color"].Value.ShouldBe("red");
        dto.Attributes["color"].HexCode.ShouldBe("#FF0000");
    }

    [Fact]
    public async Task Factory_Create_WithUnknownAttributeValue_FallsBackToDisplayValue()
    {
        var attributeType = await new AttributeTypeBuilder().WithName("size").BuildAsync();
        var variant = new ProductVariantBuilder().Build();
        variant.SetAttributes(new[]
        {
            AttributeAssignment.Create(attributeType.Id, AttributeValueIdAlias.NewId(), "Large")
        });
        var inventory = new InventoryBuilder().WithVariantId(variant.Id).Build();

        var dto = ProductVariantViewDtoFactory.Create(
            variant, inventory, new List<AttributeValueEntity>());

        dto.Attributes.Count.ShouldBe(1);
        var entry = dto.Attributes.Single();
        entry.Value.Value.ShouldBe("Large");
        entry.Value.DisplayValue.ShouldBe("Large");
        entry.Value.IsActive.ShouldBeTrue();
    }
}
