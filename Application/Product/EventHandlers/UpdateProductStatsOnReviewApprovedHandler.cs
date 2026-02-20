namespace Application.Product.EventHandlers;

public class UpdateProductStatsOnReviewApprovedHandler : INotificationHandler<ReviewApprovedEvent>
{
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductStatsOnReviewApprovedHandler> _logger;

    public UpdateProductStatsOnReviewApprovedHandler(
        IProductRepository productRepository,
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductStatsOnReviewApprovedHandler> logger)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ReviewApprovedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            // Fetch product (lightweight)
            var product = await _productRepository.GetByIdAsync(notification.ProductId, cancellationToken);
            if (product == null) return;

            // Recalculate stats
            var (reviews, totalCount) = await _reviewRepository.GetByProductIdAsync(
                notification.ProductId,
                "Approved",
                1,
                int.MaxValue, // Ideally, use an aggregation query on repo instead of fetching all
                cancellationToken);

            decimal avgRating = 0;
            if (totalCount > 0)
            {
                avgRating = (decimal)reviews.Average(r => r.Rating);
            }

            var newStats = product.Stats.UpdateReviews(totalCount, avgRating);
            product.UpdateStats(newStats);

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated stats for Product {ProductId} after review approval.", notification.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update product stats for Product {ProductId}", notification.ProductId);
        }
    }
}