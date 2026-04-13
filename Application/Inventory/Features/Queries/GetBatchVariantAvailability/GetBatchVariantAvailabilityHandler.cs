using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public class GetBatchVariantAvailabilityHandler(
    IInventoryQueryService inventoryQueryService)
    : IRequestHandler<GetBatchVariantAvailabilityQuery, ServiceResult<IReadOnlyList<VariantAvailabilityDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<VariantAvailabilityDto>>> Handle(
        GetBatchVariantAvailabilityQuery request,
        CancellationToken ct)
    {
        if (request.VariantIds == null || request.VariantIds.Count == 0)
            return ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success([]);

        var newVariantIds = request.VariantIds.Select(VariantId.From).ToList();
        var result = await inventoryQueryService.GetBatchAvailabilityAsync(
            newVariantIds,
            ct);

        return ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success(result);
    }
}