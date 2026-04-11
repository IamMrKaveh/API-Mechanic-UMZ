namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusHandler(IInventoryQueryService queryService) : IRequestHandler<GetInventoryStatusQuery, ServiceResult<InventoryStatusDto>>
{
    public async Task<ServiceResult<InventoryStatusDto>> Handle(
        GetInventoryStatusQuery request,
        CancellationToken ct)
    {
        var status = await queryService.GetInventoryStatusAsync(request.VariantId, ct);

        if (status is null)
            return ServiceResult<InventoryStatusDto>.NotFound("واریانت مورد نظر یافت نشد.");

        return ServiceResult<InventoryStatusDto>.Success(status);
    }
}