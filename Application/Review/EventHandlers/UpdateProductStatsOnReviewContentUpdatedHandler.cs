using Domain.Product.Interfaces;
using Domain.Review.Events;
using Microsoft.Extensions.Logging;

namespace Application.Review.EventHandlers;

public sealed class UpdateProductStatsOnReviewContentUpdatedHandler(
    IReviewQueryService reviewQueryService,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductStatsOnReviewContentUpdatedHandler> logger)
    : INotificationHandler<DomainEventNotification<ReviewContentUpdatedEvent>>
{
    public async Task Handle(
        DomainEventNotification<ReviewContentUpdatedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        var product = await productRepository.GetByIdAsync(evt.ProductId, ct);
        if (product is null)
        {
            logger.LogWarning(
                "Cannot recalculate stats. Product {ProductId} not found for updated review {ReviewId}.",
                evt.ProductId.Value,
                evt.ReviewId.Value,
                ct);
            return;
        }

        var summary = await reviewQueryService.GetProductReviewSummaryAsync(evt.ProductId, ct);
        var avg = summary?.AverageRating ?? 0d;
        var count = summary?.TotalReviews ?? 0;

        product.RecalculateReviewStats(avg, count);
        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
