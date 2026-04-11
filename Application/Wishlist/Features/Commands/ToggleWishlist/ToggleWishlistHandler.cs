using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler(
    IWishlistRepository wishlistRepository,
    IWishlistQueryService wishlistQueryService,
    IUnitOfWork unitOfWork,
    ILogger<ToggleWishlistHandler> logger) : IRequestHandler<ToggleWishlistCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(
        ToggleWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        try
        {
            var isInWishlist = await wishlistQueryService.IsInWishlistAsync(
                userId,
                productId,
                ct);

            if (isInWishlist)
            {
                await wishlistRepository.RemoveAsync(
                    userId,
                    productId,
                    ct);
                await unitOfWork.SaveChangesAsync(ct);

                logger.LogInformation(
                    "محصول {ProductId} از لیست علاقه‌مندی کاربر {UserId} حذف شد.",
                    request.ProductId, request.UserId);

                return ServiceResult<bool>.Success(false);
            }

            var wishlist = Domain.Wishlist.Aggregates.Wishlist.Create(
                userId,
                productId);

            await wishlistRepository.AddAsync(wishlist, ct);
            await unitOfWork.SaveChangesAsync(ct);

            logger.LogInformation(
                "محصول {ProductId} به لیست علاقه‌مندی کاربر {UserId} اضافه شد.",
                productId,
                userId);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "خطا در تغییر وضعیت علاقه‌مندی محصول {ProductId} برای کاربر {UserId}",
                productId,
                userId);
            return ServiceResult<bool>.Failure("خطای داخلی سرور.");
        }
    }
}