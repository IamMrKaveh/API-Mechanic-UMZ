namespace Application.Variant.Features.Commands.RemoveStock;

public record RemoveStockCommand(int VariantId, int Quantity, int UserId, string Notes) : IRequest<ServiceResult>;