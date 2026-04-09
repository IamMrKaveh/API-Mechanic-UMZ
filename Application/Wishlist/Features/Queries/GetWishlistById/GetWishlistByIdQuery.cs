using Application.Wishlist.Features.Shared;
using SharedKernel.Models;

namespace Application.Wishlist.Features.Queries.GetWishlistById;

public record GetWishlistByIdQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<ServiceResult<PaginatedResult<WishlistItemDto>>>;