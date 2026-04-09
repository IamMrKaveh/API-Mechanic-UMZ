namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    List<StockInItem> Items,
    Guid? UserId,
    string Reason) : IRequest<ServiceResult>;

public record StockInItem(Guid VariantId, int Quantity, string? ReferenceNumber);