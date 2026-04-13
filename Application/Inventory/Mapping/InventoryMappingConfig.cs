using Application.Inventory.Features.Shared;
using Domain.Inventory.Entities;
using Mapster;

namespace Application.Inventory.Mapping;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Inventory.Aggregates.Inventory, InventoryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity.Value)
            .Map(dest => dest.OnHand, src => src.StockQuantity.Value)
            .Map(dest => dest.ReservedQuantity, src => src.ReservedQuantity.Value)
            .Map(dest => dest.Reserved, src => src.ReservedQuantity.Value)
            .Map(dest => dest.AvailableQuantity, src => src.AvailableQuantity)
            .Map(dest => dest.Available, src => src.AvailableQuantity)
            .Map(dest => dest.AvailableStock, src => src.AvailableQuantity)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.IsInStock, src => src.IsInStock)
            .Map(dest => dest.IsLowStock, src => src.IsLowStock)
            .IgnoreNonMapped(true);

        config.NewConfig<StockLedgerEntry, StockLedgerEntryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.EventType, src => src.EventType.ToString())
            .Map(dest => dest.QuantityDelta, src => src.QuantityDelta)
            .Map(dest => dest.BalanceAfter, src => src.BalanceAfter)
            .Map(dest => dest.Note, src => src.Note)
            .Map(dest => dest.ReferenceNumber, src => src.ReferenceNumber)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .IgnoreNonMapped(true);
    }
}