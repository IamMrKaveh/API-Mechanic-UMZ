namespace Application.Inventory.Features.Commands.RemoveStock;

public record RemoveStockCommand(Guid VariantId, int Quantity, Guid UserId, string Notes) : IRequest<ServiceResult>;