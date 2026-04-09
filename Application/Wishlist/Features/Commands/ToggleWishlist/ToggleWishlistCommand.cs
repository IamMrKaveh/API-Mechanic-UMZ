namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public record ToggleWishlistCommand(Guid UserId, Guid ProductId) : IRequest<ServiceResult<bool>>;