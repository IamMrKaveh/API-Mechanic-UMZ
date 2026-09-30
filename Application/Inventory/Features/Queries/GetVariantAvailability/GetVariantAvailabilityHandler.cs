using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandler(
    IInventoryQueryService inventoryQueryService)
    : IQueryHandler<GetVariantAvailabilityQuery, VariantAvailabilityDto>
{
    public async Task<ServiceResult<VariantAvailabilityDto>> Handle(
        GetVariantAvailabilityQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);

        var statusResult = await (inventoryQueryService.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("واریانت یافت نشد.");
        if (statusResult.IsFailure) return statusResult.Error;
        var status = statusResult.Value;

        var dto = new VariantAvailabilityDto
        {
            VariantId = status.VariantId,
            IsAvailable = status.IsInStock || status.IsUnlimited,
            AvailableQuantity = status.AvailableStock,
            IsUnlimited = status.IsUnlimited,
            IsLowStock = status.IsLowStock
        };

        return ServiceResult<VariantAvailabilityDto>.Success(dto);
    }
}
