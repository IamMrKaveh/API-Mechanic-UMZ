using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Shared;
using SharedKernel.Models;

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
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}