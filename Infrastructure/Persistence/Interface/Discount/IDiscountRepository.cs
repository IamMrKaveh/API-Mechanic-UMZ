namespace Infrastructure.Persistence.Interface.Discount;

public interface IDiscountRepository
{
    Task<DiscountCode?> GetDiscountByCodeForUpdateAsync(string code);
}