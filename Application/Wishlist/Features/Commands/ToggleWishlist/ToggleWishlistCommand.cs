namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public record ToggleWishlistCommand(
    Guid ProductId)
    : ICommand<bool>;