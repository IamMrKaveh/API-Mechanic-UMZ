using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public class RemoveFromWishlistHandler(
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork,
    ILogger<RemoveFromWishlistHandler> logger) : IRequestHandler<RemoveFromWishlistCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        RemoveFromWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var item = await wishlistRepository.GetByUserAndProductAsync(userId, productId, ct);
        if (item is null)
            return ServiceResult.NotFound("آیتم در علاقه‌مندی‌ها یافت نشد.");

        await wishlistRepository.RemoveAsync(userId, productId, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Product {ProductId} removed from wishlist for user {UserId}", request.ProductId, request.UserId);
        return ServiceResult.Success();
    }
}