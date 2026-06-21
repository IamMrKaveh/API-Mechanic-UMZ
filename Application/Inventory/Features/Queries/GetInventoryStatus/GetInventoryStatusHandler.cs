using Application.Inventory.Features.Shared;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusHandler(
    IInventoryQueryService queryService,
    IVariantRepository variantRepository)
    : IRequestHandler<GetInventoryStatusQuery, ServiceResult<InventoryStatusDto>>
{
    public async Task<ServiceResult<InventoryStatusDto>> Handle(
        GetInventoryStatusQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var status = await queryService.GetInventoryStatusAsync(variantId, ct);

        if (status is not null)
            return ServiceResult<InventoryStatusDto>.Success(status);

        var variantExists = await variantRepository.ExistsAsync(variantId, ct);
        if (!variantExists)
            return ServiceResult<InventoryStatusDto>.NotFound("واریانت مورد نظر یافت نشد.");

        var emptyStatus = new InventoryStatusDto
        {
            VariantId = request.VariantId,
            StockQuantity = 0,
            ReservedQuantity = 0,
            AvailableStock = 0,
            IsInStock = false,
            IsUnlimited = false,
            IsLowStock = false
        };

        return ServiceResult<InventoryStatusDto>.Success(emptyStatus);
    }
}