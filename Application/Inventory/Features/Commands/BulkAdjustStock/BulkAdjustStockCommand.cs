namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public record BulkAdjustStockCommand(
    ICollection<Guid> VariantId,
    ICollection<int> QuantityChange,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;