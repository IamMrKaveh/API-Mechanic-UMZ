namespace Domain.Discount.Exceptions;

public sealed class DiscountNotFoundException : DomainException
{
    public string Code { get; }

    public override string ErrorCode => "DISCOUNT_NOT_FOUND";

    public DiscountNotFoundException(string code)
        : base($"کد تخفیف '{code}' یافت نشد.")
    {
        Code = code;
    }
}