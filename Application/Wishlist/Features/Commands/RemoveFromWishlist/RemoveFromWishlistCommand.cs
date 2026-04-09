namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(Guid UserId, Guid ProductId) : IRequest<ServiceResult>;