namespace Domain.Discount.Exceptions;

public sealed class DiscountNotActiveException(string code) : DomainException($"کد تخفیف '{code}' غیرفعال است.")
{
    public string Code { get; } = code;
}