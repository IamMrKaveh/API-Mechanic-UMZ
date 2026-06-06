namespace Application.Inventory.Features.Commands.RemoveStock;

public record RemoveStockCommand(
    Guid VariantId,
    int Quantity,
    string Notes) : IRequest<ServiceResult>;