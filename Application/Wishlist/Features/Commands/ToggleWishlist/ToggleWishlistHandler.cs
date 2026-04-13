using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public class ToggleWishlistHandler(
    IWishlistRepository wishlistRepository,
    IWishlistQueryService wishlistQueryService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ToggleWishlistCommand, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(
        ToggleWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        try
        {
            var isInWishlist = await wishlistQueryService.IsInWishlistAsync(userId, productId, ct);

            if (isInWishlist)
            {
                await wishlistRepository.RemoveAsync(userId, productId, ct);
                await unitOfWork.SaveChangesAsync(ct);
                return ServiceResult<bool>.Success(false);
            }

            var wishlist = Domain.Wishlist.Aggregates.Wishlist.Create(userId, productId);
            await wishlistRepository.AddAsync(wishlist, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Failure("خطا در تغییر وضعیت علاقه‌مندی.");
        }
    }
}