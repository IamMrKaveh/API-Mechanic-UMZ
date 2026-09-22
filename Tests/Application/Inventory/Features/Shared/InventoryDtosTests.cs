using Application.Inventory.Features.Shared;

namespace Tests.Application.Inventory.Features.Shared;

public class InventoryDtosTests
{
    [Fact]
    public void InventoryTransactionDto_Defaults_AreEmpty()
    {
        var dto = new InventoryTransactionDto();

        dto.TransactionType.ShouldBe(string.Empty);
        dto.QuantityChange.ShouldBe(0);
        dto.BalanceAfter.ShouldBe(0);
        dto.ProductName.ShouldBeNull();
        dto.ReferenceNumber.ShouldBeNull();
    }

    [Fact]
    public void InventoryDto_InitProperties_RoundTrip()
    {
        var dto = new InventoryDto
        {
            Id = Guid.NewGuid(), VariantId = Guid.NewGuid(), StockQuantity = 50,
            OnHand = 50, Reserved = 5, Available = 45, ReservedQuantity = 5,
            AvailableQuantity = 45, IsUnlimited = false, IsInStock = true,
            IsLowStock = false, LowStockThreshold = 5, AvailableStock = 45
        };

        dto.StockQuantity.ShouldBe(50);
        dto.Available.ShouldBe(45);
        dto.AvailableStock.ShouldBe(45);
        dto.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void StockLedgerEntryDto_Defaults_AreEmpty()
    {
        var dto = new StockLedgerEntryDto();

        dto.EventType.ShouldBe(string.Empty);
        dto.QuantityDelta.ShouldBe(0);
        dto.Note.ShouldBeNull();
    }

    [Fact]
    public void VariantAvailabilityDto_RoundTrip()
    {
        var dto = new VariantAvailabilityDto
        {
            VariantId = Guid.NewGuid(), IsAvailable = true,
            AvailableQuantity = 8, IsUnlimited = false, IsLowStock = true
        };

        dto.IsAvailable.ShouldBeTrue();
        dto.IsLowStock.ShouldBeTrue();
    }

    [Fact]
    public void LowStockItemDto_RoundTrip()
    {
        var dto = new LowStockItemDto
        {
            ProductId = Guid.NewGuid(), VariantId = Guid.NewGuid(),
            ProductName = "P", Sku = "SKU", StockQuantity = 2, LowStockThreshold = 5
        };

        dto.StockQuantity.ShouldBe(2);
        dto.LowStockThreshold.ShouldBe(5);
    }

    [Fact]
    public void OutOfStockItemDto_RoundTrip()
    {
        var dto = new OutOfStockItemDto { VariantId = Guid.NewGuid(), ProductName = "P", Sku = "S" };

        dto.ProductName.ShouldBe("P");
    }

    [Fact]
    public void InventoryStatisticsDto_RoundTrip()
    {
        var dto = new InventoryStatisticsDto
        {
            TotalVariants = 10, InStockVariants = 7, OutOfStockVariants = 2,
            LowStockVariants = 1, UnlimitedVariants = 3
        };

        dto.TotalVariants.ShouldBe(10);
        dto.UnlimitedVariants.ShouldBe(3);
    }

    [Fact]
    public void InventoryStatusDto_RoundTrip()
    {
        var dto = new InventoryStatusDto
        {
            VariantId = Guid.NewGuid(), StockQuantity = 20, ReservedQuantity = 4,
            AvailableStock = 16, IsInStock = true, IsUnlimited = false, IsLowStock = false
        };

        dto.AvailableStock.ShouldBe(16);
    }

    [Fact]
    public void WarehouseStockDto_RoundTrip()
    {
        var dto = new WarehouseStockDto
        {
            WarehouseId = Guid.NewGuid(), WarehouseName = "Main",
            VariantId = Guid.NewGuid(), Quantity = 30, ReservedQuantity = 2
        };

        dto.WarehouseName.ShouldBe("Main");
        dto.Quantity.ShouldBe(30);
    }
}
