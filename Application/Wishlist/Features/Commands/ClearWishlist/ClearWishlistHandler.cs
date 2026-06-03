using Domain.User.ValueObjects;
using Domain.Wishlist.Interfaces;

namespace Application.Wishlist.Features.Commands.ClearWishlist;

public sealed class ClearWishlistHandler(
    IWishlistRepository wishlistRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<ClearWishlistCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ClearWishlistCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        await wishlistRepository.ClearAsync(userId, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}