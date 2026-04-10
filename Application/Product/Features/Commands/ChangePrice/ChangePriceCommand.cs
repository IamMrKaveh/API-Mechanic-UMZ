namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand(
    Guid ProductId,
    Guid VariantId,
    Guid UserId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice) : IRequest<ServiceResult>;