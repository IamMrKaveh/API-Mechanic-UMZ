namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    IReadOnlyList<BulkStockInItem> Items,
    string Reason)
    : IRequest<ServiceResult>, IManualTransactionRequest;

public record BulkStockInItem(
    Guid VariantId,
    int Quantity,
    string? ReferenceNumber);