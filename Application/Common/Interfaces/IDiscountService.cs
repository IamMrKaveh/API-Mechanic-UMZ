namespace Application.Common.Interfaces;

public interface IDiscountService
{
    Task<(Domain.Discount.DiscountCode? discount, string? errorMessage)> ValidateAndGetDiscountAsync(string code, int userId, decimal orderTotal);
}