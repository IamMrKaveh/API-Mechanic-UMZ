using Application.Common.Results;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Product.Features.Commands.ChangePrice;

public record ChangePriceCommand(
    ProductId ProductId,
    VariantId VariantId,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal OriginalPrice) : IRequest<ServiceResult>;