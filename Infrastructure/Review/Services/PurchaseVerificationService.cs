using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.Services;

public sealed class PurchaseVerificationService(DBContext context) : IPurchaseVerificationService
{
    public async Task<bool> UserHasPurchasedProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
        => await context.OrderItems
            .AnyAsync(item =>
                item.ProductId == productId &&
                item.Order.UserId == userId &&
                item.Order.IsDelivered, ct);
}