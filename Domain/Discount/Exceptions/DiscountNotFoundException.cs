namespace Domain.Discount.Exceptions;

public sealed class DiscountNotFoundException(string code) : DomainException($"کد تخفیف '{code}' یافت نشد.")
{
    public string Code { get; } = code;
}