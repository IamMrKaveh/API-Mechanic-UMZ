using Application.Common.Interfaces;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public record BulkStockInCommand(
    IReadOnlyList<BulkStockInItem> Items,
    Guid? UserId,
    string Reason)
    : IRequest<ServiceResult>, IManualTransactionRequest;

public record BulkStockInItem(
    Guid VariantId,
    int Quantity,
    string? ReferenceNumber);