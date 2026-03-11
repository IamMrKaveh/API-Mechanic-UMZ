using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutPriceValidatorService
{
    ServiceResult Validate(
        IReadOnlyList<(ProductVariant Variant, int Quantity)> variantItems,
        IReadOnlyList<CheckoutItemPriceDto> expectedItems);
}