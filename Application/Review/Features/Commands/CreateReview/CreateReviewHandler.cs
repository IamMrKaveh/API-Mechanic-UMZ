using Application.Review.Configuration;
using Application.Review.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.Review.Services;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;
using Microsoft.Extensions.Options;

namespace Application.Review.Features.Commands.CreateReview;

public sealed class CreateReviewHandler(
    ReviewDomainService reviewDomainService,
    IReviewRepository reviewRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUser,
    IOptions<ReviewSettings> reviewSettings,
    IMapper mapper)
    : ICommandHandler<CreateReviewCommand, ProductReviewDto>
{
    public async Task<ServiceResult<ProductReviewDto>> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is null)
            return ServiceResult<ProductReviewDto>.Unauthorized("برای ثبت نظر ابتدا وارد شوید.");

        var productId = ProductId.From(request.ProductId);
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult<ProductReviewDto>.NotFound("محصول یافت نشد.");

        var userId = UserId.From(currentUser.UserId!.Value);
        OrderId? orderId = request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null;
        var rating = Rating.Create(request.Rating);

        var result = await reviewDomainService.SubmitReviewAsync(
            productId,
            userId,
            rating,
            request.Title,
            request.Comment,
            orderId,
            requirePurchaseVerification: reviewSettings.Value.RequirePurchaseVerification,
            ct);

        if (result.IsFailure)
            return ServiceResult<ProductReviewDto>.Failure(result.Error);

        await reviewRepository.AddAsync(result.Value, ct);

        return ServiceResult<ProductReviewDto>.Success(mapper.Map<ProductReviewDto>(result.Value));
    }
}
