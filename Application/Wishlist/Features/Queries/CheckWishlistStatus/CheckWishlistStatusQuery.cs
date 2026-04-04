using Application.Common.Results;

namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public record CheckWishlistStatusQuery(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;