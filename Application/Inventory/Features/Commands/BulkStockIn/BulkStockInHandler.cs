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
        return await _inventoryService.BulkStockInAsync(
            request.Items,
            request.UserId,
            request.SupplierReference,
            cancellationToken);
    }
}