namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandler : IRequestHandler<BulkStockInCommand, ServiceResult<BulkStockInResultDto>>
{
    private readonly IInventoryService _inventoryService;

    public BulkStockInHandler(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<ServiceResult<BulkStockInResultDto>> Handle(
        BulkStockInCommand request, CancellationToken cancellationToken)
    {
        var mappedItems = request.Items.Select(x => (x.VariantId, x.Quantity, x.Notes));

        var result = await _inventoryService.BulkStockInAsync(
            mappedItems,
            request.UserId,
            request.SupplierReference,
            cancellationToken);

        if (result.IsSucceed && result.Data != default)
        {
            var data = result.Data;

            var dto = new BulkStockInResultDto
            {
                TotalRequested = data.Total,
                SuccessCount = data.Success,
                FailedCount = data.Failed,
                Results = data.Results.Select(r => new BulkStockInItemResultDto
                {
                    VariantId = r.VariantId,
                    IsSuccess = r.IsSuccess,
                    Error = r.Error,
                    NewStock = r.NewStock ?? 0
                }).ToList()
            };

            return ServiceResult<BulkStockInResultDto>.Success(dto);
        }

        return ServiceResult<BulkStockInResultDto>.Failure(result.Error ?? "Failed", result.StatusCode);
    }
}