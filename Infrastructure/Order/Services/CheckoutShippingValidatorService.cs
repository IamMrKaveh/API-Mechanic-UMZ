using Application.Order.Features.Commands.CheckoutFromCart.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Order.Services;

public class CheckoutShippingValidatorService(
    IShippingRepository shippingRepository,
    IVariantRepository variantRepository)
    : ICheckoutShippingValidatorService
{
    public async Task<ServiceResult<Money>> ValidateAndCalculateCostAsync(
        Guid shippingId,
        decimal orderAmount,
        IReadOnlyCollection<OrderItemSnapshot> items,
        CancellationToken ct)
    {
        var shippingIdVo = ShippingId.From(shippingId);

        var shipping = await shippingRepository.GetByIdAsync(shippingIdVo, ct);
        if (shipping is null)
            return ServiceResult<Money>.NotFound("روش ارسال یافت نشد.");

        var orderTotal = Money.FromDecimal(orderAmount);
        var validation = shipping.ValidateForOrder(orderTotal);

        if (!validation.IsSuccess)
            return ServiceResult<Money>.Failure(validation.Error.Message);

        if (items is null || items.Count == 0)
        {
            var fallbackCost = shipping.CalculateCost(orderTotal);
            return ServiceResult<Money>.Success(fallbackCost);
        }

        var variantIds = items.Select(i => i.VariantId).Distinct().ToList();
        var variants = await variantRepository.GetByIdsWithShippingsAsync(variantIds, ct);

        var multiplierByVariant = new Dictionary<VariantId, decimal>();
        foreach (var variant in variants)
        {
            var assignment = variant.Shippings.FirstOrDefault(s => s.ShippingId == shippingIdVo);
            multiplierByVariant[variant.Id] = assignment is not null && assignment.ShippingMultiplier > 0
                ? assignment.ShippingMultiplier
                : 1m;
        }

        var costItems = items
            .Select(i => new ShippingCostItem(
                i.VariantId,
                multiplierByVariant.TryGetValue(i.VariantId, out var m) ? m : 1m,
                i.Quantity))
            .ToList();

        var cost = shipping.CalculateCostForCart(orderTotal, costItems);
        return ServiceResult<Money>.Success(cost);
    }
}