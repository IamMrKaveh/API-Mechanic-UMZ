using Application.Common.Results;

namespace Application.Inventory.Features.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid VariantId,
    int QuantityChange,
    Guid UserId,
    string Reason) : IRequest<ServiceResult>;