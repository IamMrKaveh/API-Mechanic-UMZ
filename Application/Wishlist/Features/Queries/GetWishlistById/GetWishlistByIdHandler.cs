using Application.Common.Models;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public class GetWishlistByIdHandler
    : IRequestHandler<GetWishlistByIdQuery, ServiceResult<PaginatedResult<WishlistItemDto>>>
{
    private readonly IWishlistQueryService _wishlistQueryService;

    public GetWishlistByIdHandler(IWishlistQueryService wishlistQueryService)
    {
        _wishlistQueryService = wishlistQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<WishlistItemDto>>> Handle(
        GetWishlistByIdQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _wishlistQueryService.GetPagedAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}