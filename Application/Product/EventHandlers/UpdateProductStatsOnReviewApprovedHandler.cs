using Domain.Product.Interfaces;
using Domain.Review.Events;
using Domain.Review.Interfaces;

namespace Application.Product.EventHandlers;

public class UpdateProductStatsOnReviewApprovedHandler(
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateProductStatsOnReviewApprovedHandler> logger) : INotificationHandler<ReviewApprovedEvent>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<UpdateProductStatsOnReviewApprovedHandler> _logger = logger;

    public async Task Handle(ReviewApprovedEvent notification, CancellationToken ct)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(notification.ProductId, ct);
            if (product == null) return;

            var (reviews, totalCount) = await _reviewRepository.GetByProductIdAsync(
                notification.ProductId,
                "Approved",
                1,
                int.MaxValue,
                ct);

            decimal avgRating = 0;
            if (totalCount > 0)
                avgRating = (decimal)reviews.Average(r => r.Rating);

            var newStats = product.Stats.UpdateReviews(totalCount, avgRating);
            product.UpdateStats(newStats);

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Updated stats for Product {ProductId} after review approval.",
                notification.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product stats for Product {ProductId}", notification.ProductId);
        }
    }
}