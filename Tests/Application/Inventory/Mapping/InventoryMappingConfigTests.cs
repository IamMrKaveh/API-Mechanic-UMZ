using Application.Inventory.Features.Shared;
using Application.Inventory.Mapping;
using Domain.Inventory.Entities;
using Domain.Variant.ValueObjects;
using Mapster;

namespace Tests.Application.Inventory.Mapping;

public class InventoryMappingConfigTests
{
    private readonly TypeAdapterConfig _config;
    private readonly IMapper _mapper;

    public InventoryMappingConfigTests()
    {
        _config = new TypeAdapterConfig();
        new InventoryMappingConfig().Register(_config);
        _mapper = new Mapper(_config);
    }

    [Fact]
    public void Map_Inventory_ToInventoryDto_MapsQuantitiesAndFlags()
    {
        var inventory = new InventoryBuilder().WithInitialStock(20).Build();

        var dto = _mapper.Map<InventoryDto>(inventory);

        dto.Id.ShouldBe(inventory.Id.Value);
        dto.VariantId.ShouldBe(inventory.VariantId.Value);
        dto.StockQuantity.ShouldBe(20);
        dto.OnHand.ShouldBe(20);
        dto.Reserved.ShouldBe(0);
        dto.ReservedQuantity.ShouldBe(0);
        dto.Available.ShouldBe(20);
        dto.AvailableQuantity.ShouldBe(20);
        dto.AvailableStock.ShouldBe(20);
        dto.IsUnlimited.ShouldBeFalse();
        dto.IsInStock.ShouldBeTrue();
        dto.IsLowStock.ShouldBeFalse();
    }

    [Fact]
    public void Map_UnlimitedInventory_MapsUnlimitedFlag()
    {
        var inventory = new InventoryBuilder().AsUnlimited().Build();

        var dto = _mapper.Map<InventoryDto>(inventory);

        dto.IsUnlimited.ShouldBeTrue();
        dto.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public void Map_StockLedgerEntry_ToDto_MapsAllFields()
    {
        var variantId = VariantId.NewId();
        var entry = StockLedgerEntry.StockIn(variantId, 5, 15, 1000m, DateTime.UtcNow, "REF-1", "note");

        var dto = _mapper.Map<StockLedgerEntryDto>(entry);

        dto.Id.ShouldBe(entry.Id.Value);
        dto.VariantId.ShouldBe(variantId.Value);
        dto.EventType.ShouldBe(entry.EventType.ToString());
        dto.QuantityDelta.ShouldBe(5);
        dto.BalanceAfter.ShouldBe(15);
        dto.Note.ShouldBe("note");
        dto.ReferenceNumber.ShouldBe("REF-1");
        dto.CreatedAt.ShouldBe(entry.CreatedAt);
    }

    [Fact]
    public void Register_DoesNotThrow_AndCompiles()
    {
        var config = new TypeAdapterConfig();

        Should.NotThrow(() => new InventoryMappingConfig().Register(config));
        Should.NotThrow(() => config.Compile());
    }
}
