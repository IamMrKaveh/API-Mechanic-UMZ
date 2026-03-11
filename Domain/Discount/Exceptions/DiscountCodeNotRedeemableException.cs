using Domain.Discount.ValueObjects;

namespace Domain.Discount.Exceptions;

public sealed class DiscountCodeNotRedeemableException(DiscountCodeId id, string code) : Exception($"Discount code '{code}' (ID: {id}) is not redeemable. It may be inactive, expired, or have reached its usage limit.")
{
}