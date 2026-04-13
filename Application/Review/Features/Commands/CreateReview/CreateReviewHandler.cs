using Application.Review.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Review.Interfaces;
using Domain.Review.Services;
using Domain.Review.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Review.Features.Commands.CreateReview;

public sealed class CreateReviewHandler(
    ReviewDomainService reviewDomainService,
    IReviewRepository reviewRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IRequestHandler<CreateReviewCommand, ServiceResult<ProductReviewDto>>
{
    public async Task<ServiceResult<ProductReviewDto>> Handle(CreateReviewCommand request, CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult<ProductReviewDto>.NotFound("محصول یافت نشد.");

        var userId = UserId.From(request.UserId);
        OrderId? orderId = request.OrderId.HasValue ? OrderId.From(request.OrderId.Value) : null;
        var rating = Rating.Create(request.Rating);

        var result = await reviewDomainService.SubmitReviewAsync(
            productId,
            userId,
            rating,
            request.Title,
            request.Comment,
            orderId,
            requirePurchaseVerification: false,
            hasExistingReviewCheck: async (uid, pid, oid, c) =>
                await reviewRepository.UserHasReviewedProductAsync(uid, pid, oid, c),
            ct);

        if (result.IsFailure)
            return ServiceResult<ProductReviewDto>.Failure(result.Error.Message);

        await reviewRepository.AddAsync(result.Value, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult<ProductReviewDto>.Success(mapper.Map<ProductReviewDto>(result.Value));
    }
}