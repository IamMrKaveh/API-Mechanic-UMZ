namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandler(
    ICacheService cacheService,
    IInventoryQueryService inventoryQueryService,
    ILogger<GetVariantAvailabilityHandler> logger)
        : IRequestHandler<GetVariantAvailabilityQuery, ServiceResult<VariantAvailabilityDto>>
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly IInventoryQueryService _inventoryQueryService = inventoryQueryService;
    private readonly ILogger<GetVariantAvailabilityHandler> _logger = logger;

    private static string CacheKey(int variantId) => $"inventory:availability:{variantId}";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<ServiceResult<VariantAvailabilityDto>> Handle(
        GetVariantAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var key = CacheKey(request.VariantId);

        try
        {
            var cached = await _cacheService.GetAsync<VariantAvailabilityDto>(key);
            if (cached != null)
                return ServiceResult<VariantAvailabilityDto>.Success(cached);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for variant {VariantId}, falling back to DB", request.VariantId);
        }

        var status = await _inventoryQueryService.GetVariantStatusAsync(request.VariantId, cancellationToken);
        if (status == null)
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
            await _cacheService.SetAsync(key, dto, CacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for variant {VariantId}", request.VariantId);
        }

        return ServiceResult<VariantAvailabilityDto>.Success(dto);
    }
}