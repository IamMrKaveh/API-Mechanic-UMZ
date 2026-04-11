using Domain.Common.ValueObjects;
using Domain.Discount.ValueObjects;

namespace Domain.Common.Services;

public interface IPricingService
{
    Money CalculateFinalPrice(Money basePrice, DiscountValue? discount);
}