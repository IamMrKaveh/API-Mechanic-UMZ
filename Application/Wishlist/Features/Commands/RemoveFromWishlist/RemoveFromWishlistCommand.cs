namespace Application.Wishlist.Features.Commands.RemoveFromWishlist;

public record RemoveFromWishlistCommand(int UserId, int ProductId) : IRequest<ServiceResult>;