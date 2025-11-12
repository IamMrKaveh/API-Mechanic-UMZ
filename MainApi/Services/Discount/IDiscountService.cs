namespace MainApi.Services.Discount
{
    public interface IDiscountService
    {
        Task<(TDiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal);
    }
}