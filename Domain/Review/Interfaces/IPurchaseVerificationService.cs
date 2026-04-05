using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Review.Interfaces;

public interface IPurchaseVerificationService
{
    Task<bool> UserHasPurchasedProductAsync(UserId userId, ProductId productId, CancellationToken ct = default);
}