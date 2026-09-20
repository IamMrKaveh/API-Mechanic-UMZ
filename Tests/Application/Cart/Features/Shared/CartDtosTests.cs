using Application.Attribute.Features.Shared;
using Application.Cart.Features.Shared;

namespace Tests.Application.Cart.Features.Shared;

public class CartDtosTests
{
    [Fact]
    public void CartDetailDto_Defaults_AreEmpty()
    {
        var dto = new CartDetailDto();

        dto.Id.ShouldBe(default(Guid));
        dto.UserId.ShouldBeNull();
        dto.GuestToken.ShouldBeNull();
        dto.IsCheckedOut.ShouldBeFalse();
        dto.Items.ShouldNotBeNull();
        dto.Items.ShouldBeEmpty();
        dto.TotalPrice.ShouldBe(0m);
        dto.TotalItems.ShouldBe(0);
        dto.PriceChanges.ShouldNotBeNull();
        dto.PriceChanges.ShouldBeEmpty();
    }

    [Fact]
    public void CartItemDto_Defaults_AreEmpty()
    {
        var dto = new CartItemDto();

        dto.ProductName.ShouldBe(string.Empty);
        dto.Sku.ShouldBe(string.Empty);
        dto.Quantity.ShouldBe(0);
        dto.Attributes.ShouldBeNull();
        dto.ProductIcon.ShouldBeNull();
    }

    [Fact]
    public void CartItemDetailDto_Defaults_AreEmpty()
    {
        var dto = new CartItemDetailDto();

        dto.ProductName.ShouldBe(string.Empty);
        dto.VariantSku.ShouldBeNull();
        dto.ProductImage.ShouldBeNull();
        dto.Attributes.ShouldBeNull();
        dto.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void CartPriceChangeDto_StoresValues()
    {
        var variantId = Guid.NewGuid();
        var dto = new CartPriceChangeDto(variantId, "Brake Pad", 100m, 120m);

        dto.VariantId.ShouldBe(variantId);
        dto.ProductName.ShouldBe("Brake Pad");
        dto.OldPrice.ShouldBe(100m);
        dto.NewPrice.ShouldBe(120m);
    }

    [Fact]
    public void CartSummaryDto_InitProperties_RoundTrip()
    {
        var dto = new CartSummaryDto { ItemCount = 3, TotalQuantity = 5, TotalPrice = 500m };

        dto.ItemCount.ShouldBe(3);
        dto.TotalQuantity.ShouldBe(5);
        dto.TotalPrice.ShouldBe(500m);
    }

    [Fact]
    public void CartCheckoutValidationDto_Defaults_AreValidShape()
    {
        var dto = new CartCheckoutValidationDto();

        dto.IsValid.ShouldBeFalse();
        dto.Errors.ShouldNotBeNull();
        dto.Errors.ShouldBeEmpty();
        dto.PriceChanges.ShouldBeEmpty();
        dto.StockIssues.ShouldBeEmpty();
    }

    [Fact]
    public void CartCheckoutValidationDto_WithErrors_PreservesLists()
    {
        var dto = new CartCheckoutValidationDto
        {
            IsValid = false,
            Errors = new List<string> { "Out of stock" },
            PriceChanges = new List<CartPriceChangeDto> { new(Guid.NewGuid(), "P", 1m, 2m) },
            StockIssues = new List<CartStockIssueDto>
            {
                new() { VariantId = Guid.NewGuid(), ProductName = "P", RequestedQuantity = 5, AvailableStock = 2 }
            }
        };

        dto.Errors.Count.ShouldBe(1);
        dto.PriceChanges.Count.ShouldBe(1);
        dto.StockIssues.Count.ShouldBe(1);
        dto.StockIssues[0].RequestedQuantity.ShouldBe(5);
    }

    [Fact]
    public void CartStockIssueDto_RequiredProductName_RoundTrip()
    {
        var dto = new CartStockIssueDto
        {
            VariantId = Guid.NewGuid(),
            ProductName = "Oil Filter",
            RequestedQuantity = 10,
            AvailableStock = 3
        };

        dto.ProductName.ShouldBe("Oil Filter");
        dto.RequestedQuantity.ShouldBe(10);
        dto.AvailableStock.ShouldBe(3);
    }

    [Fact]
    public void SyncCartPricesResult_Defaults_HasNoChanges()
    {
        var dto = new SyncCartPricesResult();

        dto.HasChanges.ShouldBeFalse();
        dto.PriceChanges.ShouldBeEmpty();
        dto.RemovedVariantIds.ShouldBeEmpty();
    }

    [Fact]
    public void CartItemDto_AttributeDictionary_AcceptsValues()
    {
        var dto = new CartItemDto
        {
            Attributes = new Dictionary<string, AttributeValueDto>
            {
                ["color"] = new AttributeValueDto { Value = "red", DisplayValue = "Red" }
            }
        };

        dto.Attributes["color"].Value.ShouldBe("red");
    }
}
