using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ClearWishlist;

public sealed class ClearWishlistHandler(
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService)
    : ICommandHandler<ClearWishlistCommand>
{
    public async Task<ServiceResult> Handle(ClearWishlistCommand request, CancellationToken ct)
    {
        var userId = UserId.From(currentUserService.UserId.Value);
        await wishlistRepository.ClearAsync(userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}