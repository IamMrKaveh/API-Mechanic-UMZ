using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutPriceValidatorService(OrderDomainService orderDomainService) : ICheckoutPriceValidatorService
{
    private readonly OrderDomainService _orderDomainService = orderDomainService;

    public ServiceResult Validate(
        IReadOnlyList<(ProductVariant Variant, int Quantity)> variantItems,
        IReadOnlyList<CheckoutItemPriceDto> expectedItems)
    {
        var priceExpectations = variantItems
            .Select(vi => (
                vi.Variant,
                ExpectedPrice: expectedItems
                    .FirstOrDefault(e => e.VariantId == vi.Variant.Id)?.ExpectedPrice
                    ?? vi.Variant.SellingPrice.Amount))
            .ToList();

        var result = _orderDomainService.ValidatePriceIntegrity(priceExpectations);
        if (!result.IsValid)
            return ServiceResult.Failure(result.GetErrorsSummary());

        return ServiceResult.Success();
    }
}