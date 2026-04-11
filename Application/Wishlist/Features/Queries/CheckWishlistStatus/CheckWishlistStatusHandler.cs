using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public class CheckWishlistStatusHandler(IWishlistQueryService wishlistQueryService) : IRequestHandler<CheckWishlistStatusQuery, ServiceResult<bool>>
{
    public async Task<ServiceResult<bool>> Handle(
        CheckWishlistStatusQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var isInWishlist = await wishlistQueryService.IsInWishlistAsync(
            userId,
            productId,
            ct);

        return ServiceResult<bool>.Success(isInWishlist);
    }
}