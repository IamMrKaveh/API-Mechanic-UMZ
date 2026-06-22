namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public record CheckWishlistStatusQuery(
    Guid UserId,
    Guid ProductId)
    : IQuery<bool>;