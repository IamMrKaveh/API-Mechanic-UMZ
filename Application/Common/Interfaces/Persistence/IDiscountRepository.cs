namespace Application.Common.Interfaces.Persistence;

public interface IDiscountRepository
{
    Task<Domain.Discount.DiscountCode?> GetDiscountByCodeForUpdateAsync(string code);
}