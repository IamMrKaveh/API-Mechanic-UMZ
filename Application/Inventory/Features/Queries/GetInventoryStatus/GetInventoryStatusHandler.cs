using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusHandler(IInventoryQueryService queryService)
    : IRequestHandler<GetInventoryStatusQuery, ServiceResult<InventoryStatusDto>>
{
    public async Task<ServiceResult<InventoryStatusDto>> Handle(
        GetInventoryStatusQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var status = await queryService.GetInventoryStatusAsync(variantId, ct);

        if (status is null)
            return ServiceResult<InventoryStatusDto>.NotFound("واریانت مورد نظر یافت نشد.");

        return ServiceResult<InventoryStatusDto>.Success(status);
    }
}