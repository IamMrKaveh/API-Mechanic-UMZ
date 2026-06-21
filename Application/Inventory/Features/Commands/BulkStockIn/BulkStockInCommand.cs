namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    IReadOnlyList<BulkStockInItem> Items,
    string Reason)
    : ICommand, IManualTransactionRequest;

public record BulkStockInItem(
    Guid VariantId,
    int Quantity,
    string? ReferenceNumber);