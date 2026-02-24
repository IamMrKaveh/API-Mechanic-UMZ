namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public class GetInventoryStatusHandler
    : IRequestHandler<GetInventoryStatusQuery, ServiceResult<InventoryStatusDto>>
{
    private readonly IInventoryQueryService _queryService;

    public GetInventoryStatusHandler(IInventoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<InventoryStatusDto>> Handle(
        GetInventoryStatusQuery request,
        CancellationToken cancellationToken)
    {
        var status = await _queryService.GetInventoryStatusAsync(request.VariantId, cancellationToken);

        if (status is null)
            return ServiceResult<InventoryStatusDto>.Failure("واریانت مورد نظر یافت نشد.");

        return ServiceResult<InventoryStatusDto>.Success(status);
    }
}