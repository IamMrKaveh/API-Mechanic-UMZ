namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(
    Guid ProductId)
    : ICommand;