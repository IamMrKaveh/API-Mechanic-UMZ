using Application.Inventory.Features.Queries.GetVariantAvailability;

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

        var result = await inventoryQueryService.GetBatchAvailabilityAsync(
            request.VariantIds.Distinct().ToList(),
            ct);

        return ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success(result);
    }
}