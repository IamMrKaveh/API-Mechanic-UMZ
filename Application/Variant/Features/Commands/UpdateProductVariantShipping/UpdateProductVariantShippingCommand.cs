using Application.Common.Results;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public record UpdateProductVariantShippingCommand(
    ProductVariantId VariantId,
    decimal ShippingMultiplier,
    List<int> EnabledShippingIds,
    UserId UserId) : IRequest<ServiceResult>;