using Application.Common.Results;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetVariantAvailability;

namespace Application.Inventory.Features.Queries.GetBatchVariantAvailability;

public class GetBatchVariantAvailabilityHandler(
    IInventoryQueryService inventoryQueryService)
    : IRequestHandler<GetBatchVariantAvailabilityQuery, ServiceResult<IReadOnlyList<VariantAvailabilityDto>>>
{
    private readonly IInventoryQueryService _inventoryQueryService = inventoryQueryService;

    public async Task<ServiceResult<IReadOnlyList<VariantAvailabilityDto>>> Handle(
        GetBatchVariantAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        if (request.VariantIds == null || request.VariantIds.Count == 0)
            return ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success(
                Array.Empty<VariantAvailabilityDto>());

        var result = await _inventoryQueryService.GetBatchAvailabilityAsync(
            request.VariantIds.Distinct().ToList(),
            cancellationToken);

        return ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success(result);
    }
}