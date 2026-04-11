using Application.Wishlist.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public class GetWishlistByIdHandler(IWishlistQueryService wishlistQueryService)
        : IRequestHandler<GetWishlistByIdQuery, ServiceResult<PaginatedResult<WishlistItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<WishlistItemDto>>> Handle(
        GetWishlistByIdQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await wishlistQueryService.GetPagedAsync(
            userId,
            ct);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}