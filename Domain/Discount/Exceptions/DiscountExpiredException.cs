namespace Domain.Discount.Exceptions;

public sealed class DiscountExpiredException(string discountCode, DateTime expiryDate) : DomainException($"کد تخفیف {discountCode} در تاریخ {expiryDate:yyyy/MM/dd} منقضی شده است.")
{
    public string DiscountCode { get; } = discountCode;
    public DateTime ExpiryDate { get; } = expiryDate;
}