using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public interface ICheckoutStockValidatorService
{
    ServiceResult Validate(IReadOnlyList<(ProductVariant Variant, int Quantity)> variantItems);
}