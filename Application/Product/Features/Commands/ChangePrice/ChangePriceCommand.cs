using Application.Common.Results;

namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand(
    Guid ProductId,
    Guid VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice) : IRequest<ServiceResult>;