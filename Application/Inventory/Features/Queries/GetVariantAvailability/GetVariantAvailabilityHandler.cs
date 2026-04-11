namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandler(
    ICacheService cacheService,
    IInventoryQueryService inventoryQueryService,
    ILogger<GetVariantAvailabilityHandler> logger)
        : IRequestHandler<GetVariantAvailabilityQuery, ServiceResult<VariantAvailabilityDto>>
{
    private static string CacheKey(int variantId) => $"inventory:availability:{variantId}";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<ServiceResult<VariantAvailabilityDto>> Handle(
        GetVariantAvailabilityQuery request,
        CancellationToken ct)
    {
        var key = CacheKey(request.VariantId);

        try
        {
            var cached = await cacheService.GetAsync<VariantAvailabilityDto>(key);
            if (cached is not null)
                return ServiceResult<VariantAvailabilityDto>.Success(cached);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache read failed for variant {VariantId}, falling back to DB", request.VariantId);
        }

        var status = await inventoryQueryService.GetVariantStatusAsync(request.VariantId, ct);
        if (status is null)
            return ServiceResult<VariantAvailabilityDto>.NotFound("واریانت یافت نشد.");

        var dto = new VariantAvailabilityDto
        {
            VariantId = status.VariantId,
            OnHand = status.StockQuantity,
            Reserved = status.ReservedQuantity,
            Available = status.AvailableStock,
            IsInStock = status.IsInStock,
            IsUnlimited = status.IsUnlimited,
            LastUpdated = DateTime.UtcNow
        };

        try
        {
            await cacheService.SetAsync(key, dto, CacheTtl, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for variant {VariantId}", request.VariantId);
        }

        return ServiceResult<VariantAvailabilityDto>.Success(dto);
    }
}