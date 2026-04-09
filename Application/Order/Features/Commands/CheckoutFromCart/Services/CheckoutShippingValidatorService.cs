using Domain.Common.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public class CheckoutShippingValidatorService(IShippingRepository shippingRepository)
    : ICheckoutShippingValidatorService
{
    public async Task<ServiceResult<Money>> ValidateAndCalculateCostAsync(
        Guid shippingId, decimal orderAmount, CancellationToken ct)
    {
        var shipping = await shippingRepository.GetByIdAsync(ShippingId.From(shippingId), ct);
        if (shipping is null)
            return ServiceResult<Money>.NotFound("روش ارسال یافت نشد.");

        var orderTotal = Money.FromDecimal(orderAmount);
        var validation = shipping.ValidateForOrder(orderTotal);

        if (!validation.IsSuccess)
            return ServiceResult<Money>.Failure(validation.Error.Message);

        var cost = shipping.CalculateCost(orderTotal);
        return ServiceResult<Money>.Success(cost);
    }
}