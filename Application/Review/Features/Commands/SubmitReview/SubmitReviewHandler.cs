using Application.Common.Results;
using Application.Review.Features.Shared;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Product.Interfaces;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;

namespace Application.Review.Features.Commands.SubmitReview;

public class SubmitReviewHandler(
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ILogger<SubmitReviewHandler> logger) : IRequestHandler<SubmitReviewCommand, ServiceResult<ProductReviewDto>>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IReviewRepository _reviewRepository = reviewRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ILogger<SubmitReviewHandler> _logger = logger;

    public async Task<ServiceResult<ProductReviewDto>> Handle(
        SubmitReviewCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, ct);

        if (product is null)
            return ServiceResult<ProductReviewDto>.NotFound("محصول یافت نشد.");

        if (product.IsDeleted || !product.IsActive)
            return ServiceResult<ProductReviewDto>.NotFound("محصول حذف شده یا غیرفعال هست");

        if (await _reviewRepository.UserHasReviewedProductAsync(request.UserId, request.ProductId, request.OrderId, ct))
            return ServiceResult<ProductReviewDto>.Conflict("شما قبلاً برای این محصول نظر ثبت کرده‌اید.");

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

            _logger.LogInformation(
                "Review {ReviewId} submitted by user {UserId} for product {ProductId}. Verified: {IsVerified}",
                review.Id,
                request.UserId,
                request.ProductId,
                isVerifiedPurchase);

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
            return ServiceResult<ProductReviewDto>.Unexpected(ex.Message);
        }
    }
}