namespace Domain.Discount.Results;

public sealed class DiscountApplicationResult
{
    public bool IsSuccess { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public int? DiscountCodeId { get; private set; }
    public Money? DiscountMoney { get; private set; }
    public string? Error { get; private set; }

    private DiscountApplicationResult() { }

    public static DiscountApplicationResult Success(decimal amount) =>
        new() { IsSuccess = true, DiscountAmount = amount };

    public static DiscountApplicationResult Success(int discountCodeId, Money discountAmount) =>
        new()
        {
            IsSuccess = true,
            DiscountCodeId = discountCodeId,
            DiscountAmount = discountAmount.Amount,
            DiscountMoney = discountAmount
        };

    public static DiscountApplicationResult Failed(string error) =>
        new() { IsSuccess = false, Error = error };
}