namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public record UpdateVariantShippingCommand(
    Guid VariantId,
    decimal ShippingMultiplier,
    ICollection<Guid> EnabledShippingIds,
    Guid UserId) : IRequest<ServiceResult>;