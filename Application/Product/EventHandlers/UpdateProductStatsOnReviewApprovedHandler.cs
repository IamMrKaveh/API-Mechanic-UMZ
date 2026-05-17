using Domain.Product.Interfaces;
using Domain.Review.Events;

namespace Application.Product.EventHandlers;

public sealed class UpdateProductStatsOnReviewApprovedHandler(
    IProductRepository productRepository,
    IAuditService auditService) : INotificationHandler<DomainEventNotification<ReviewApprovedEvent>>
{
    public async Task Handle(DomainEventNotification<ReviewApprovedEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var product = await productRepository.GetByIdAsync(domainEvent.ProductId, ct);
        if (product is null)
            return;

        await auditService.LogSystemEventAsync(
            "ReviewApproved",
            $"نظر برای محصول {domainEvent.ProductId.Value} تایید شد. امتیاز: {domainEvent.Rating.Value}",
            ct);
    }
}