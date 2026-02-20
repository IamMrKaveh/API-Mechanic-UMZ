namespace Application.Variant.Features.Commands.UpdateProductVariantShipping;

public record UpdateProductVariantShippingCommand : IRequest<ServiceResult>
{
    public int VariantId { get; init; }
    public decimal ShippingMultiplier { get; init; } = 1;
    public List<int> EnabledShippingMethodIds { get; init; } = new();
    public int UserId { get; init; }
}