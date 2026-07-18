namespace Application.Wishlist.Features.Queries.CheckWishlistStatus;

public record CheckWishlistStatusQuery(
    Guid ProductId)
    : IQuery<bool>;