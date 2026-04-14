using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;
using Domain.Shipping.Services;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Shipping.QueryServices;

public sealed class ShippingQueryService(
    DBContext context,
    ShippingDomainService shippingDomainService) : IShippingQueryService
{
    public async Task<ShippingDetailDto?> GetShippingDetailAsync(
        ShippingId shippingId, CancellationToken ct = default)
    {
        var shipping = await context.Shippings
            .AsNoTracking()
            .Where(s => s.Id == shippingId)
            .Select(s => new ShippingDetailDto
            {
                Id = s.Id.Value,
                Name = s.Name.Value,
                Description = s.Description,
                Cost = s.Cost.Amount,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                IsFreeShippingEnabled = s.FreeShipping.IsEnabled,
                FreeShippingThreshold = s.FreeShipping.ThresholdAmount != null
                    ? s.FreeShipping.ThresholdAmount.Amount : (decimal?)null,
                MinDeliveryDays = s.MinDeliveryDays,
                MaxDeliveryDays = s.MaxDeliveryDays,
                SortOrder = s.SortOrder,
                RowVersion = s.RowVersion.ToBase64(),
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        return shipping;
    }

    public async Task<IReadOnlyList<ShippingListItemDto>> GetAllShippingsAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var query = context.Shippings.AsNoTracking().AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        var items = await query
            .OrderBy(s => s.SortOrder)
            .Select(s => new ShippingListItemDto
            {
                Id = s.Id.Value,
                Name = s.Name.Value,
                Description = s.Description,
                Cost = s.Cost.Amount,
                IsActive = s.IsActive,
                IsDefault = s.IsDefault,
                IsFreeShippingEnabled = s.FreeShipping.IsEnabled,
                FreeShippingThreshold = s.FreeShipping.ThresholdAmount != null
                    ? s.FreeShipping.ThresholdAmount.Amount : (decimal?)null,
                SortOrder = s.SortOrder
            })
            .ToListAsync(ct);

        return items.AsReadOnly();
    }

    public async Task<IReadOnlyList<AvailableShippingDto>> GetAvailableShippingsForOrderAsync(
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
            var calcResult = shippingDomainService.CalculateCost(shipping, orderAmount);
            if (!calcResult.IsSuccess) continue;

            var isFree = calcResult.IsFreeShipping;
            var finalCost = calcResult.Cost?.Amount ?? 0;

            result.Add(new AvailableShippingDto
            {
                Id = shipping.Id.Value,
                Name = shipping.Name.Value,
                Description = shipping.Description,
                Cost = finalCost,
                IsFree = isFree,
                IsDefault = shipping.IsDefault,
                DeliveryTimeDisplay = GetDeliveryTimeDisplay(shipping)
            });
        }

        return result.AsReadOnly();
    }

    public async Task<AvailableShippingDto?> CalculateShippingCostAsync(
        ShippingId shippingId, Money orderAmount, CancellationToken ct = default)
    {
        var shipping = await context.Shippings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shippingId && s.IsActive, ct);

        if (shipping is null) return null;

        var calcResult = shippingDomainService.CalculateCost(shipping, orderAmount);
        if (!calcResult.IsSuccess) return null;

        return new AvailableShippingDto
        {
            Id = shipping.Id.Value,
            Name = shipping.Name.Value,
            Description = shipping.Description,
            Cost = calcResult.Cost?.Amount ?? 0,
            IsFree = calcResult.IsFreeShipping,
            IsDefault = shipping.IsDefault,
            DeliveryTimeDisplay = GetDeliveryTimeDisplay(shipping)
        };
    }

    public async Task<IReadOnlyList<AvailableShippingDto>> GetAvailableShippingsAsync(
        Money orderAmount, CancellationToken ct = default)
        => await GetAvailableShippingsForOrderAsync(orderAmount, ct);

    public async Task<IReadOnlyList<AvailableShippingDto>> GetAvailableShippingsForVariantsAsync(
        IEnumerable<Guid> variantIds, CancellationToken ct = default)
    {
        var variantIdList = variantIds.ToList();

        var enabledShippingIds = await context.ProductVariantShippings
            .AsNoTracking()
            .Where(pvs => variantIdList.Contains(pvs.ProductVariantId.Value))
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
            Description = s.Description,
            Cost = s.Cost.Amount,
            IsFree = false,
            IsDefault = s.IsDefault,
            DeliveryTimeDisplay = GetDeliveryTimeDisplay(s)
        }).ToList().AsReadOnly();
    }

    private static string GetDeliveryTimeDisplay(Domain.Shipping.Aggregates.Shipping shipping)
    {
        if (shipping.MinDeliveryDays.HasValue && shipping.MaxDeliveryDays.HasValue)
            return $"{shipping.MinDeliveryDays} تا {shipping.MaxDeliveryDays} روز کاری";
        if (shipping.MinDeliveryDays.HasValue)
            return $"از {shipping.MinDeliveryDays} روز کاری";
        return "زمان تحویل متغیر";
    }
}