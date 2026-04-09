namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public record CommitStockForOrderCommand(
    List<OrderItemStockCommit> Items,
    string OrderNumber) : IRequest<ServiceResult>;

public record OrderItemStockCommit(Guid VariantId, int Quantity, Guid? OrderItemId);