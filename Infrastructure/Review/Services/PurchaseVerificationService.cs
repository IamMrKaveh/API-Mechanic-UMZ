using Application.Review.Configuration;
using Domain.Order.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.Review.Services;

public sealed class PurchaseVerificationService(DBContext context, IOptions<ReviewSettings> settings) : IPurchaseVerificationService
{
    public async Task<bool> UserHasPurchasedProductAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-settings.Value.PurchaseReviewWindowDays);

        return await context.OrderItems
            .AnyAsync(item =>
                item.ProductId == productId &&
                item.Order.UserId == userId &&
                item.Order.Status == OrderStatusValue.Delivered &&
                item.Order.DeliveredAt != null &&
                item.Order.DeliveredAt >= cutoff, ct);
    }
}
