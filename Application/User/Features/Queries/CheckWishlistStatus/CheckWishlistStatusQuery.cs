namespace Application.User.Features.Queries.CheckWishlistStatus;

public record CheckWishlistStatusQuery(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;