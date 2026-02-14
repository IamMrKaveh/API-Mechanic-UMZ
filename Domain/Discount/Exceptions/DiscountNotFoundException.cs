namespace Domain.Discount.Exceptions;

public sealed class DiscountNotFoundException : DomainException
{
    public string Code { get; }

    public DiscountNotFoundException(string code)
        : base($"کد تخفیف '{code}' یافت نشد.")
    {
        Code = code;
    }
}