using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.AddToWishlist;

public class AddToWishlistHandler(
    IWishlistRepository wishlistRepository,
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<AddToWishlistHandler> logger) : IRequestHandler<AddToWishlistCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        AddToWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null || !product.IsActive)
            return ServiceResult.NotFound("محصول یافت نشد.");

        if (await wishlistRepository.ExistsAsync(userId, productId, ct))
            return ServiceResult.Conflict("این محصول قبلاً به علاقه‌مندی‌ها اضافه شده است.");

        var wishlistItem = Domain.Wishlist.Aggregates.Wishlist.Create(userId, productId);
        await wishlistRepository.AddAsync(wishlistItem, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Product {ProductId} added to wishlist for user {UserId}", request.ProductId, request.UserId);
        return ServiceResult.Success();
    }
}