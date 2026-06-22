using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler(
    IWishlistRepository wishlistRepository,
    IWishlistQueryService wishlistQueryService,
    IAuditService auditService)
    : ICommandHandler<ToggleWishlistCommand, bool>
{
    public async Task<ServiceResult<bool>> Handle(ToggleWishlistCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId); var productId = ProductId.From(request.ProductId);

        var isInWishlist = await wishlistQueryService.IsInWishlistAsync(userId, productId, ct);
        bool added;

        if (isInWishlist)
        {
            await wishlistRepository.RemoveAsync(userId, productId, ct);
            added = false;
        }
        else
        {
            var wishlist = Domain.Wishlist.Aggregates.Wishlist.Create(userId, productId);
            await wishlistRepository.AddAsync(wishlist, ct);
            added = true;
        }

        await auditService.LogSystemEventAsync(
            "ToggleWishlist",
            $"وضعیت علاقه‌مندی محصول {productId.Value} برای کاربر {userId.Value} تغییر یافت. اکنون: {(added ? "افزوده" : "حذف")}.",
            ct);

        return ServiceResult<bool>.Success(added);
    }
}