namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    List<BulkStockInItemDto> Items,
    int UserId,
    string? SupplierReference = null)
    : IRequest<ServiceResult<BulkStockInResultDto>>;