namespace Application.Review.Features.Commands.SubmitReview;

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
        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);
        if (product == null || product.IsDeleted || !product.IsActive)
            return ServiceResult<ProductReviewDto>.Failure("محصول یافت نشد.", 404);

        if (await _reviewRepository.UserHasReviewedProductAsync(request.UserId, request.ProductId, request.OrderId, ct))
            return ServiceResult<ProductReviewDto>.Failure("شما قبلاً برای این محصول نظر ثبت کرده‌اید.", 400);

        // Security / Trust Check
        var isVerifiedPurchase = await _reviewRepository.UserHasPurchasedProductAsync(request.UserId, request.ProductId, ct);

        try
        {
            var review = ProductReview.Create(
                request.ProductId,
                request.UserId,
                request.Rating,
                request.Title,
                request.Comment,
                isVerifiedPurchase,
                request.OrderId);

            await _reviewRepository.AddAsync(review, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation("Review {ReviewId} submitted by user {UserId} for product {ProductId}. Verified: {IsVerified}",
                review.Id, request.UserId, request.ProductId, isVerifiedPurchase);

            var dto = new ProductReviewDto
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

            return ServiceResult<ProductReviewDto>.Success(dto);
        }
        catch (DomainException ex)
        {
            return ServiceResult<ProductReviewDto>.Failure(ex.Message, 400);
        }
    }
}