using Domain.Review;

namespace Application.Product.Features.Commands.SubmitReview;

public class SubmitReviewHandler : IRequestHandler<SubmitReviewCommand, ServiceResult<ProductReviewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubmitReviewHandler> _logger;

    public SubmitReviewHandler(
        IProductRepository productRepository,
        IReviewRepository reviewRepository,
        IUnitOfWork unitOfWork,
        ILogger<SubmitReviewHandler> logger)
    {
        _productRepository = productRepository;
        _reviewRepository = reviewRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<ProductReviewDto>> Handle(
        SubmitReviewCommand request, CancellationToken ct)
    {
        // 1. Verify product exists and is active
        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product == null || product.IsDeleted || !product.IsActive)
            return ServiceResult<ProductReviewDto>.Failure("محصول یافت نشد.", 404);

        // 2. Check duplicate review (application-level cross-aggregate check)
        if (await _reviewRepository.UserHasReviewedProductAsync(
                request.UserId, request.ProductId, request.OrderId, ct))
            return ServiceResult<ProductReviewDto>.Failure("شما قبلاً برای این محصول نظر ثبت کرده‌اید.");

        // 3. Check verified purchase (application-level concern)
        var isVerifiedPurchase = await _reviewRepository.UserHasPurchasedProductAsync(
            request.UserId, request.ProductId, ct);

        // 4. Create review via Product aggregate
        try
        {
            var review = product.AddReview(
                request.UserId,
                request.Rating,
                request.Title,
                request.Comment,
                isVerifiedPurchase,
                request.OrderId);

            _productRepository.Update(product);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Review {ReviewId} submitted by user {UserId} for product {ProductId}",
                review.Id, request.UserId, request.ProductId);

            var dto = MapToDto(review);
            return ServiceResult<ProductReviewDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ProductReviewDto>.Failure(ex.Message);
        }
    }

    private static ProductReviewDto MapToDto(ProductReview review)
    {
        return new ProductReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            OrderId = review.OrderId,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            Status = review.Status,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            CreatedAt = review.CreatedAt
        };
    }
}