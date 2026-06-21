namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public record CommitStockForOrderCommand(
    IReadOnlyList<OrderItemStockCommit> Items,
    string OrderNumber)
    : ICommand, IManualTransactionRequest;

public record OrderItemStockCommit(Guid VariantId, int Quantity, Guid? OrderItemId);