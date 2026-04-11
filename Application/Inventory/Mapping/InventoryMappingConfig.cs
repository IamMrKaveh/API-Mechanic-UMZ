using Application.Inventory.Features.Commands.AdjustStock;
using Application.Inventory.Features.Commands.BulkAdjustStock;
using Application.Inventory.Features.Commands.BulkStockIn;
using Application.Inventory.Features.Commands.RecordDamage;
using Application.Inventory.Features.Commands.ReverseInventoryTransaction;
using Application.Inventory.Features.Shared;
using Domain.Inventory.Aggregates;
using Mapster;

namespace Application.Inventory.Mapping;

public class InventoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Inventory, InventoryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity)
            .Map(dest => dest.ReservedQuantity, src => src.ReservedQuantity)
            .Map(dest => dest.AvailableQuantity, src => src.AvailableQuantity)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.IsInStock, src => src.IsInStock)
            .Map(dest => dest.IsLowStock, src => src.IsLowStock)
            .Map(dest => dest.LowStockThreshold, src => src.LowStockThreshold)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<ReverseInventoryDto, ReverseInventoryCommand>()
            .Map(dest => dest.VariantId, src => src.VariantId)
            .Map(dest => dest.IdempotencyKey, src => src.IdempotencyKey)
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);

        config.NewConfig<AdjustStockDto, AdjustStockCommand>()
            .Map(dest => dest.VariantId, src => src.VariantId)
            .Map(dest => dest.QuantityChange, src => src.QuantityChange)
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);

        config.NewConfig<BulkAdjustStockDto, BulkAdjustStockCommand>()
            .Map(dest => dest.Items, src => src.Items)
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);

        config.NewConfig<RecordDamageDto, RecordDamageCommand>()
            .Map(dest => dest.VariantId, src => src.VariantId)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);

        config.NewConfig<BulkStockInDto, BulkStockInCommand>()
            .Map(dest => dest.Items, src => src.Items)
            .Map(dest => dest.Reason, src => src.Reason)
            .IgnoreNonMapped(true);
    }
}