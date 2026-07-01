namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public record UpdateVariantShippingCommand(
    Guid VariantId,
    decimal ShippingMultiplier,
    decimal WeightGrams,
    ICollection<Guid> EnabledShippingIds)
    : ICommand;