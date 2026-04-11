using Application.Review.Features.Shared;
using Domain.Common.Exceptions;
using Domain.Order.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Aggregates;
using Domain.Review.Interfaces;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Commands.SubmitReview;

public class SubmitReviewHandler(
    IProductRepository productRepository,
    IReviewRepository reviewRepository,
    IUnitOfWork unitOfWork,
    ILogger<SubmitReviewHandler> logger) : IRequestHandler<SubmitReviewCommand, ServiceResult<ProductReviewDto>>
{
    public async Task<ServiceResult<ProductReviewDto>> Handle(
        SubmitReviewCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(request.UserId);
        var orderId = OrderId.From(request.OrderId.Value);

        var product = await productRepository.GetByIdAsync(productId, ct);

        if (product is null)
            return ServiceResult<ProductReviewDto>.NotFound("محصول یافت نشد.");

        if (product.IsDeleted || !product.IsActive)
            return ServiceResult<ProductReviewDto>.NotFound("محصول حذف شده یا غیرفعال هست");

        if (await reviewRepository.UserHasReviewedProductAsync(
            userId,
            productId,
            orderId,
            ct))
            return ServiceResult<ProductReviewDto>.Conflict("شما قبلاً برای این محصول نظر ثبت کرده‌اید.");

        var isVerifiedPurchase = await reviewRepository.UserHasPurchasedProductAsync(userId, productId, ct);

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

            await reviewRepository.AddAsync(review, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "Review {ReviewId} submitted by user {UserId} for product {ProductId}. Verified: {IsVerified}",
                review.Id,
                request.UserId,
                request.ProductId,
                isVerifiedPurchase);

            var dto = new ProductReviewDto
            {
                Id = review.Id.Value,
                ProductId = review.ProductId.Value,
                UserId = review.UserId.Value,
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
            return ServiceResult<ProductReviewDto>.Failure(ex.Message);
        }
    }
}