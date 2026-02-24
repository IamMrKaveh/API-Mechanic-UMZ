namespace Domain.Discount.Exceptions;

public sealed class DiscountExpiredException : DomainException
{
    public string DiscountCode { get; }
    public DateTime ExpiryDate { get; }

    public DiscountExpiredException(string discountCode, DateTime expiryDate)
        : base($"کد تخفیف {discountCode} در تاریخ {expiryDate:yyyy/MM/dd} منقضی شده است.")
    {
        DiscountCode = discountCode;
        ExpiryDate = expiryDate;
    }
}