namespace Domain.Review.Interfaces;

public interface IPurchaseVerificationService
{
    Task<bool> UserHasPurchasedProductAsync(int userId, int productId, CancellationToken ct = default);
}