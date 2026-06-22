namespace Application.Wishlist.Features.Commands.ClearWishlist;

public record ClearWishlistCommand(
    Guid UserId)
    : ICommand;