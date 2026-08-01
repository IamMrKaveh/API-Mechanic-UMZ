using Domain.Product.Interfaces;
using Domain.Review.Events;
using Microsoft.Extensions.Logging;

namespace Application.Review.EventHandlers;

public sealed class UpdateProductStatsOnReviewApprovedHandler(
    IReviewQueryService reviewQueryService,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductStatsOnReviewApprovedHandler> logger)
    : INotificationHandler<DomainEventNotification<ReviewApprovedEvent>>
{
    public async Task Handle(
        DomainEventNotification<ReviewApprovedEvent> notification,
        CancellationToken ct)
    {
        var evt = notification.DomainEvent;

        var product = await productRepository.GetByIdAsync(evt.ProductId, ct);
        if (product is null)
        {
            logger.LogWarning(
                "Cannot recalculate stats. Product {ProductId} not found for approved review {ReviewId}.",
                evt.ProductId.Value,
                evt.ReviewId.Value,
                ct);
            return;
        }

        var summary = await reviewQueryService.GetProductReviewSummaryAsync(evt.ProductId, ct);

        if (summary is null)
        {
            product.RecalculateReviewStats(0d, 0);
        }
        else
        {
            product.RecalculateReviewStats(summary.AverageRating, summary.TotalReviews);
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
