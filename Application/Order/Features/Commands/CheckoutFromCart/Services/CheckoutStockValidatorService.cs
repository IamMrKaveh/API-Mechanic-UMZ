using Domain.Variant.Aggregates;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public sealed class CheckoutStockValidatorService(IInventoryReservationService inventoryReservationService) : ICheckoutStockValidatorService
{
    private readonly IInventoryReservationService _inventoryReservationService = inventoryReservationService;

    public ServiceResult Validate(IReadOnlyList<(ProductVariant Variant, int Quantity)> variantItems)
    {
        var result = _inventoryReservationService.ValidateBatchAvailability(variantItems);
        if (!result.IsValid)
            return ServiceResult.Failure(result.GetErrorsSummary());

        return ServiceResult.Success();
    }
}