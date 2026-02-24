namespace Application.Variant.Features.Commands.AddStock;

public record AddStockCommand(int VariantId, int Quantity, int UserId, string Notes) : IRequest<ServiceResult>;