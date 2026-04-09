using Application.Wishlist.Features.Shared;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public class GetWishlistByIdHandler(IWishlistQueryService wishlistQueryService)
        : IRequestHandler<GetWishlistByIdQuery, ServiceResult<PaginatedResult<WishlistItemDto>>>
{
    private readonly IWishlistQueryService _wishlistQueryService = wishlistQueryService;

    public async Task<ServiceResult<PaginatedResult<WishlistItemDto>>> Handle(
        GetWishlistByIdQuery request,
        CancellationToken ct)
    {
        var result = await _wishlistQueryService.GetPagedAsync(
            request.UserId,
            ct);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}