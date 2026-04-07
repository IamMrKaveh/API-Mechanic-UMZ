using Domain.Common.Exceptions;
using Domain.Discount.ValueObjects;

namespace Domain.Discount.Exceptions;

public sealed class DiscountCodeNotRedeemableException : DomainException
{
    public DiscountCodeId Id { get; }
    public string Code { get; }

    public override string ErrorCode => "DISCOUNT_CODE_NOT_REDEEMABLE";

    public DiscountCodeNotRedeemableException(DiscountCodeId id, string code)
        : base($"Discount code '{code}' (ID: {id}) is not redeemable. It may be inactive, expired, or have reached its usage limit.")
    {
        Id = id;
        Code = code;
    }
}