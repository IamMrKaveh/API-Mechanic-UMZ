using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;
using Domain.Shipping.Services;
using Domain.Shipping.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Shipping.QueryServices;

public sealed class ShippingQueryService(
    DBContext context,
    ShippingDomainService shippingDomainService) : IShippingQueryService
{
    public async Task<ShippingDto?> GetShippingDetailAsync(
        ShippingId shippingId, CancellationToken ct = default)
    {
        var shipping = await context.Shippings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shippingId, ct);

        if (shipping is null) return null;

        return new ShippingDto
        {
            Id = shipping.Id.Value,
            Name = shipping.Name.Value,
            Description = shipping.Description,
            BaseCost = shipping.BaseCost.Amount,
            EstimatedDeliveryTime = shipping.EstimatedDeliveryTime,
            MinDeliveryDays = shipping.DeliveryTime.MinDays,
            MaxDeliveryDays = shipping.DeliveryTime.MaxDays,
            IsActive = shipping.IsActive,
            IsDefault = shipping.IsDefault,
            SortOrder = shipping.SortOrder,
            FreeShippingThreshold = shipping.FreeShipping.IsEnabled
                ? shipping.FreeShipping.ThresholdAmount?.Amount
                : null,
            MinOrderAmount = shipping.OrderRange.MinOrderAmount?.Amount,
            MaxOrderAmount = shipping.OrderRange.MaxOrderAmount?.Amount,
            MaxWeight = shipping.MaxWeight,
            CreatedAt = shipping.CreatedAt,
            UpdatedAt = shipping.UpdatedAt,
            RowVersion = null
        };
    }

    public async Task<IReadOnlyList<ShippingListItemDto>> GetAllShippingsAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var query = context.Shippings.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var shippings = await query
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return shippings.Select(s => new ShippingListItemDto
        {
            Id = s.Id.Value,
            Name = s.Name.Value,
            BaseCost = s.BaseCost.Amount,
            IsActive = s.IsActive,
            IsDefault = s.IsDefault,
            SortOrder = s.SortOrder,
            DeliveryTimeDisplay = s.GetDeliveryTimeDisplay()
        }).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ShippingListItemDto>> GetAvailableShippingsForOrderAsync(
        Money orderAmount, CancellationToken ct = default)
    {
        var activeShippings = await context.Shippings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return activeShippings
            .Where(s => s.IsAvailableForOrder(orderAmount))
            .Select(s => new ShippingListItemDto
            {
                Id = s.Id.Value,
                Name = s.Name.Value,
                BaseCost = s.BaseCost.Amount,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                SortOrder = s.SortOrder,
                DeliveryTimeDisplay = s.GetDeliveryTimeDisplay()
            }).ToList().AsReadOnly();
    }

    public async Task<ShippingCostResultDto> CalculateShippingCostAsync(
        ShippingId shippingId, Money orderAmount, CancellationToken ct = default)
    {
        var shipping = await context.Shippings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shippingId && s.IsActive, ct);

        if (shipping is null)
            return new ShippingCostResultDto();

        var cost = shipping.CalculateCost(orderAmount);
        var isFree = cost.IsZero();

        return new ShippingCostResultDto
        {
            ShippingId = shipping.Id.Value,
            ShippingName = shipping.Name.Value,
            Cost = cost.Amount,
            IsFree = isFree,
            MinDeliveryDays = shipping.DeliveryTime.MinDays,
            MaxDeliveryDays = shipping.DeliveryTime.MaxDays
        };
    }

    public async Task<IReadOnlyList<AvailableShippingDto>> GetAvailableShippingsAsync(
        Money orderAmount, CancellationToken ct = default)
    {
        var activeShippings = await context.Shippings
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var result = new List<AvailableShippingDto>();

        foreach (var shipping in activeShippings)
        {
            var calcResult = shippingDomainService.CalculateShippingCost(shipping, orderAmount);
            if (calcResult.IsSuccess is false) continue;

            result.Add(new AvailableShippingDto
            {
                Id = shipping.Id.Value,
                Name = shipping.Name.Value,
                Cost = calcResult.Cost?.Amount ?? 0,
                IsFree = calcResult.IsFreeShipping,
                IsDefault = shipping.IsDefault,
                DeliveryTimeDisplay = shipping.GetDeliveryTimeDisplay()
            });
        }

        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<AvailableShippingDto>> GetAvailableShippingsForVariantsAsync(
        IEnumerable<Guid> variantIds, CancellationToken ct = default)
    {
        var variantIdList = variantIds.ToList();

        var enabledShippingIds = await context.ProductVariantShippings
            .AsNoTracking()
            .Where(pvs => variantIdList.Contains(pvs.ShippingId.Value))
            .Select(pvs => pvs.ShippingId.Value)
            .Distinct()
            .ToListAsync(ct);

        var shippings = await context.Shippings
            .AsNoTracking()
            .Where(s => s.IsActive && enabledShippingIds.Contains(s.Id.Value))
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        return shippings.Select(s => new AvailableShippingDto
        {
            Id = s.Id.Value,
            Name = s.Name.Value,
            Cost = s.BaseCost.Amount,
            IsFree = false,
            IsDefault = s.IsDefault,
            DeliveryTimeDisplay = s.GetDeliveryTimeDisplay()
        }).ToList().AsReadOnly();
    }
}