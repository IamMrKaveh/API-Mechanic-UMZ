using Application.Common.Results;
using Domain.Order.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Order.Features.Commands.CheckoutFromCart.Services;

public class CheckoutPriceValidatorService(IVariantRepository variantRepository)
    : ICheckoutPriceValidatorService
{
    public async Task<ServiceResult> ValidateAsync(List<OrderItemSnapshot> items, CancellationToken ct)
    {
        foreach (var item in items)
        {
            var variant = await variantRepository.GetByIdAsync(
                ProductVariantId.From(item.VariantId), ct);

            if (variant is null) continue;

            if (Math.Abs(variant.Price.Amount - item.UnitPrice.Amount) > 1)
                return ServiceResult.Failure(
                    $"قیمت محصول {item.ProductName} تغییر کرده است. لطفاً سبد خرید را بروزرسانی کنید.");
        }

        return ServiceResult.Success();
    }
}