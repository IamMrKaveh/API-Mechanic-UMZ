using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public class RemoveFromWishlistHandler(
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<RemoveFromWishlistCommand>
{
    public async Task<ServiceResult> Handle(
        RemoveFromWishlistCommand request,
        CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        var productId = ProductId.From(request.ProductId);

        var item = await wishlistRepository.GetByUserAndProductAsync(userId, productId, ct);
        if (item is null)
            return ServiceResult.Success();

        await wishlistRepository.RemoveAsync(userId, productId, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}