using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandler(
    ICacheService cacheService,
    IInventoryQueryService inventoryQueryService,
    IAuditService auditService)
    : IRequestHandler<GetVariantAvailabilityQuery, ServiceResult<VariantAvailabilityDto>>
{
    private static string CacheKey(Guid variantId) => $"inventory:availability:{variantId}";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    public async Task<ServiceResult<VariantAvailabilityDto>> Handle(
        GetVariantAvailabilityQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var key = CacheKey(variantId.Value);

        try
        {
            var cached = await cacheService.GetAsync<VariantAvailabilityDto>(key);
            if (cached is not null)
                return ServiceResult<VariantAvailabilityDto>.Success(cached);
        }
        catch
        {
        }

        var status = await inventoryQueryService.GetVariantStatusAsync(request.VariantId, ct);
        if (status is null)
            return ServiceResult<VariantAvailabilityDto>.NotFound("واریانت یافت نشد.");

        var dto = new VariantAvailabilityDto
        {
            VariantId = status.VariantId,
            IsAvailable = status.IsInStock || status.IsUnlimited,
            AvailableQuantity = status.AvailableStock,
            IsUnlimited = status.IsUnlimited,
            IsLowStock = status.IsLowStock
        };

        try
        {
            await cacheService.SetAsync(key, dto, CacheTtl, ct);
        }
        catch
        {
        }

        return ServiceResult<VariantAvailabilityDto>.Success(dto);
    }
}