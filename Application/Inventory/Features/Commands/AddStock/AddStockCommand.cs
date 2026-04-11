namespace Application.Inventory.Features.Commands.AddStock;

public record AddStockCommand(
    Guid VariantId,
    int Quantity,
    Guid UserId,
    string Notes) : IRequest<ServiceResult>;