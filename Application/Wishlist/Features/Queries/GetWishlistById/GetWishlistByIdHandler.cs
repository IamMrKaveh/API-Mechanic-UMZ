using Application.Wishlist.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public class GetWishlistByIdHandler(
    IWishlistQueryService wishlistQueryService,
    ICurrentUserService currentUserService)
        : IQueryHandler<GetWishlistByIdQuery, PaginatedResult<WishlistItemDto>>
{
    private const int DefaultPageSize = 10;

    public async Task<ServiceResult<PaginatedResult<WishlistItemDto>>> Handle(
        GetWishlistByIdQuery request,
        CancellationToken ct)
    {
        var effectiveId = request.TargetUserId ?? currentUserService.UserId
            ?? throw new InvalidOperationException("User context not resolved.");

        var userId = UserId.From(effectiveId);

        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : DefaultPageSize;

        var result = await wishlistQueryService.GetPagedAsync(userId, page, pageSize, ct);

        return ServiceResult<PaginatedResult<WishlistItemDto>>.Success(result);
    }
}