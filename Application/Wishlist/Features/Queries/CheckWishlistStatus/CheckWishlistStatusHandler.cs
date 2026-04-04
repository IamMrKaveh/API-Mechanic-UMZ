using Application.Common.Results;

namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public class CheckWishlistStatusHandler : IRequestHandler<CheckWishlistStatusQuery, ServiceResult<bool>>
{
    private readonly IWishlistQueryService _wishlistQueryService;

    public CheckWishlistStatusHandler(IWishlistQueryService wishlistQueryService)
    {
        _wishlistQueryService = wishlistQueryService;
    }

    public async Task<ServiceResult<bool>> Handle(
        CheckWishlistStatusQuery request,
        CancellationToken cancellationToken)
    {
        var isInWishlist = await _wishlistQueryService.IsInWishlistAsync(
            request.UserId, request.ProductId, cancellationToken);

        return ServiceResult<bool>.Success(isInWishlist);
    }
}