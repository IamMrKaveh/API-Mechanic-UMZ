using Application.Wishlist.Features.Shared;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public record GetWishlistByIdQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<WishlistItemDto>>>;