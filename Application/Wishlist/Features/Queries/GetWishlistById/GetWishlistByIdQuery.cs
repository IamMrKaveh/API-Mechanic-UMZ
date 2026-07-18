using Application.Wishlist.Features.Shared;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public record GetWishlistByIdQuery : IPageQuery<WishlistItemDto>
{
    public Guid? TargetUserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;

    public GetWishlistByIdQuery() { }

    public GetWishlistByIdQuery(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public GetWishlistByIdQuery(Guid targetUserId, int page, int pageSize)
    {
        TargetUserId = targetUserId;
        Page = page;
        PageSize = pageSize;
    }
}