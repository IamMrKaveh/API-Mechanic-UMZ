using Domain.Product.Interfaces;
using Domain.Review.Events;

namespace Application.Product.EventHandlers;

public sealed class UpdateProductStatsOnReviewApprovedHandler(
    IProductRepository productRepository,
    IAuditService auditService) : INotificationHandler<ReviewApprovedEvent>
{
    public async Task Handle(ReviewApprovedEvent notification, CancellationToken ct)
    {
        var product = await productRepository.GetByIdAsync(notification.ProductId, ct);
        if (product is null)
            return;

        await auditService.LogSystemEventAsync(
            "ReviewApproved",
            $"نظر برای محصول {notification.ProductId.Value} تایید شد. امتیاز: {notification.Rating.Value}",
            ct);
    }
}