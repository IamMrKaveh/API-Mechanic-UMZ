namespace Domain.Discount.Exceptions;

public sealed class DiscountNotActiveException : DomainException
{
    public string Code { get; }

    public override string ErrorCode => "DISCOUNT_NOT_ACTIVE";

    public DiscountNotActiveException(string code)
        : base($"کد تخفیف '{code}' غیرفعال است.")
    {
        Code = code;
    }
}