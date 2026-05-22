using Domain.Discount.ValueObjects;

namespace Domain.Discount.Exceptions;

public sealed class DiscountCodeNotRedeemableException(DiscountCodeId id, string code) : DomainException($"Discount code '{code}' (ID: {id}) is not redeemable. It may be inactive, expired, or have reached its usage limit.")
{
    public DiscountCodeId Id { get; } = id;
    public string Code { get; } = code;

    public override string ErrorCode => "DISCOUNT_CODE_NOT_REDEEMABLE";
}