namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand(
    Guid ProductId,
    Guid VariantId,
    Guid UserId,
    decimal SellingPrice,
    decimal OriginalPrice) : IRequest<ServiceResult>;