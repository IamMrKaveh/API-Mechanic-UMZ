using Application.Wishlist.Features.Shared;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public record GetWishlistByIdQuery(Guid UserId) : IRequest<ServiceResult<PaginatedResult<WishlistItemDto>>>;