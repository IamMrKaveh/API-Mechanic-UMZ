using Application.Common.Models;

namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public record CheckWishlistStatusQuery(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;