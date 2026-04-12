namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    IReadOnlyList<BulkStockInItem> Items,
    Guid? UserId,
    string Reason) : IRequest<ServiceResult>;

public record BulkStockInItem(
    Guid VariantId,
    int Quantity,
    string? ReferenceNumber);