using Domain.Variant.Events;

namespace Tests.DomainTest.Product;

public class ProductVariantTests
{
    [Fact]
    public void Create_Should_Initialize_With_Correct_Defaults()
    {
        var variant = new ProductVariantBuilder().Build();

        Assert.True(variant.IsActive);
        Assert.False(variant.IsDeleted);
        Assert.Equal(0, variant.ReservedQuantity);
        Assert.Equal(20, variant.StockQuantity);
    }

    [Fact]
    public void Create_Should_Raise_ProductVariantCreatedEvent()
    {
        var variant = new ProductVariantBuilder().Build();

        Assert.Contains(variant.DomainEvents, e => e is ProductVariantCreatedEvent);
    }

    [Fact]
    public void AvailableStock_Should_Return_Stock_Minus_Reserved()
    {
        var variant = new ProductVariantBuilder()
            .WithStock(20)
            .Build();

        variant.Reserve(5);

        Assert.Equal(15, variant.AvailableStock);
    }

    [Fact]
    public void AvailableStock_Should_Return_MaxValue_When_Unlimited()
    {
        var variant = new ProductVariantBuilder()
            .WithUnlimited(true)
            .Build();

        Assert.Equal(int.MaxValue, variant.AvailableStock);
    }

    [Fact]
    public void IsInStock_Should_Be_True_When_AvailableStock_Positive()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();

        Assert.True(variant.IsInStock);
    }

    [Fact]
    public void IsInStock_Should_Be_False_When_No_Stock_And_Not_Unlimited()
    {
        var variant = new ProductVariantBuilder().WithStock(0).Build();

        Assert.False(variant.IsInStock);
    }

    [Fact]
    public void IsInStock_Should_Be_True_When_Unlimited()
    {
        var variant = new ProductVariantBuilder().WithUnlimited(true).WithStock(0).Build();

        Assert.True(variant.IsInStock);
    }

    [Fact]
    public void HasDiscount_Should_Be_True_When_OriginalPrice_Greater_Than_SellingPrice()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(80_000)
            .WithOriginalPrice(100_000)
            .Build();

        Assert.True(variant.HasDiscount);
    }

    [Fact]
    public void HasDiscount_Should_Be_False_When_Prices_Equal()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(100_000)
            .WithOriginalPrice(100_000)
            .Build();

        Assert.False(variant.HasDiscount);
    }

    [Fact]
    public void DiscountPercentage_Should_Calculate_Correctly()
    {
        var variant = new ProductVariantBuilder()
            .WithSellingPrice(80_000)
            .WithOriginalPrice(100_000)
            .Build();

        Assert.Equal(20m, variant.DiscountPercentage);
    }

    [Fact]
    public void CanFulfill_Should_Return_True_When_Stock_Sufficient()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();

        Assert.True(variant.CanFulfill(5));
    }

    [Fact]
    public void CanFulfill_Should_Return_False_When_Stock_Insufficient()
    {
        var variant = new ProductVariantBuilder().WithStock(3).Build();

        Assert.False(variant.CanFulfill(5));
    }

    [Fact]
    public void CanFulfill_Should_Return_True_When_Unlimited()
    {
        var variant = new ProductVariantBuilder().WithUnlimited(true).WithStock(0).Build();

        Assert.True(variant.CanFulfill(1000));
    }

    [Fact]
    public void AdjustStock_Should_Increase_When_Positive()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();

        variant.AdjustStock(5);

        Assert.Equal(15, variant.StockQuantity);
    }

    [Fact]
    public void AdjustStock_Should_Decrease_When_Negative()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();

        variant.AdjustStock(-3);

        Assert.Equal(7, variant.StockQuantity);
    }

    [Fact]
    public async Task AdjustStock_Should_Throw_When_Result_Is_Negative()
    {
        var variant = new ProductVariantBuilder().WithStock(5).Build();

        await Assert.ThrowsAsync<DomainException>(() => variant.AdjustStock(-10));
    }

    [Fact]
    public void AddStock_Should_Increase_StockQuantity()
    {
        var variant = new ProductVariantBuilder().WithStock(10).Build();

        variant.AddStock(5);

        Assert.Equal(15, variant.StockQuantity);
    }

    [Fact]
    public async void AddStock_Should_Throw_When_Quantity_Is_Zero_Or_Negative()
    {
        var variant = new ProductVariantBuilder().Build();

        await Assert.ThrowsAsync<ArgumentException>(() => variant.AddStock(0));
        await Assert.ThrowsAsync<ArgumentException>(() => variant.AddStock(-1));
    }

    [Fact]
    public void Reserve_Should_Increase_ReservedQuantity()
    {
        var variant = new ProductVariantBuilder().WithStock(20).Build();

        variant.Reserve(5);

        Assert.Equal(5, variant.ReservedQuantity);
    }

    [Fact]
    public async void Reserve_Should_Throw_When_Insufficient_AvailableStock()
    {
        var variant = new ProductVariantBuilder().WithStock(3).Build();

        await Assert.ThrowsAsync<DomainException>(() => variant.Reserve(5));
    }

    [Fact]
    public void Reserve_Should_Not_Change_Stock_When_Unlimited()
    {
        var variant = new ProductVariantBuilder().WithUnlimited(true).Build();

        variant.Reserve(100);

        Assert.Equal(0, variant.ReservedQuantity);
    }

    [Fact]
    public void Release_Should_Decrease_ReservedQuantity()
    {
        var variant = new ProductVariantBuilder().WithStock(20).Build();
        variant.Reserve(10);

        variant.Release(5);

        Assert.Equal(5, variant.ReservedQuantity);
    }

    [Fact]
    public void Release_Should_Not_Go_Below_Zero()
    {
        var variant = new ProductVariantBuilder().WithStock(20).Build();
        variant.Reserve(3);

        variant.Release(10);

        Assert.Equal(0, variant.ReservedQuantity);
    }

    [Fact]
    public void ConfirmReservation_Should_Decrease_Both_Reserved_And_Stock()
    {
        var variant = new ProductVariantBuilder().WithStock(20).Build();
        variant.Reserve(5);

        variant.ConfirmReservation(5);

        Assert.Equal(0, variant.ReservedQuantity);
        Assert.Equal(15, variant.StockQuantity);
    }

    [Fact]
    public void SetUnlimited_Should_Change_IsUnlimited()
    {
        var variant = new ProductVariantBuilder().WithUnlimited(false).Build();

        variant.SetUnlimited(true);

        Assert.True(variant.IsUnlimited);
    }

    [Fact]
    public void SetUnlimited_Should_Be_Idempotent()
    {
        var variant = new ProductVariantBuilder().WithUnlimited(true).Build();
        variant.ClearDomainEvents();

        variant.SetUnlimited(true);

        Assert.DoesNotContain(variant.DomainEvents, e => e is VariantUnlimitedChangedEvent);
    }

    [Fact]
    public void SetPricing_Should_Update_Prices()
    {
        var variant = new ProductVariantBuilder().Build();

        variant.SetPricing(60_000, 110_000, 130_000);

        Assert.Equal(60_000, variant.PurchasePrice.Amount);
        Assert.Equal(110_000, variant.SellingPrice.Amount);
        Assert.Equal(130_000, variant.OriginalPrice.Amount);
    }

    [Fact]
    public void SetPricing_Should_Throw_When_SellingPrice_Less_Than_PurchasePrice()
    {
        var variant = new ProductVariantBuilder().Build();

        await Assert.ThrowsAsync<DomainException>(() => variant.SetPricing(100_000, 80_000, 120_000));
    }

    [Fact]
    public async Task SetPricing_Should_Throw_When_SellingPrice_Exceeds_OriginalPrice()
    {
        var variant = new ProductVariantBuilder().Build();

        await Assert.ThrowsAsync<DomainException>(() => variant.SetPricing(50_000, 150_000, 100_000));
    }

    [Fact]
    public void SetPricing_Should_Raise_PriceChangedEvent_When_Selling_Price_Changes()
    {
        var variant = new ProductVariantBuilder().WithSellingPrice(100_000).Build();
        variant.ClearDomainEvents();

        variant.SetPricing(60_000, 110_000, 130_000);

        Assert.Contains(variant.DomainEvents, e => e is VariantPriceChangedEvent);
    }

    [Fact]
    public void IsLowStock_Should_Be_True_When_AvailableStock_Below_Threshold()
    {
        var variant = new ProductVariantBuilder()
            .WithStock(3)
            .WithLowStockThreshold(5)
            .Build();

        Assert.True(variant.IsLowStock);
    }

    [Fact]
    public void IsOutOfStock_Should_Be_True_When_AvailableStock_Is_Zero()
    {
        var variant = new ProductVariantBuilder().WithStock(0).Build();

        Assert.True(variant.IsOutOfStock);
    }

    [Fact]
    public void Activate_Should_Set_IsActive_To_True()
    {
        var variant = new ProductVariantBuilder().Build();
        variant.Deactivate();

        variant.Activate();

        Assert.True(variant.IsActive);
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_To_False()
    {
        var variant = new ProductVariantBuilder().Build();

        variant.Deactivate();

        Assert.False(variant.IsActive);
    }

    [Fact]
    public void SoftDelete_Should_Set_IsDeleted_And_Deactivate()
    {
        var variant = new ProductVariantBuilder().Build();

        variant.SoftDelete(deletedBy: 5);

        Assert.True(variant.IsDeleted);
        Assert.False(variant.IsActive);
        Assert.Equal(5, variant.DeletedBy);
    }

    [Fact]
    public async Task UpdateDetails_Should_Throw_When_ShippingMultiplier_Is_Zero_Or_Negative()
    {
        var variant = new ProductVariantBuilder().Build();

        await Assert.ThrowsAsync<DomainException>(() => variant.UpdateDetails("SKU-TEST", 0));
        await Assert.ThrowsAsync<DomainException>(() => variant.UpdateDetails("SKU-TEST", -1));
    }

    [Fact]
    public void UpdateDetails_Should_Update_Sku_And_ShippingMultiplier()
    {
        var variant = new ProductVariantBuilder().Build();

        variant.UpdateDetails("SKU-NEW-999", 2.5m);

        Assert.Equal("SKU-NEW-999", variant.Sku.Value);
        Assert.Equal(2.5m, variant.ShippingMultiplier);
    }
}