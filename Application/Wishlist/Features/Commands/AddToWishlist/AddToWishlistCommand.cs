namespace Application.Wishlist.Features.Commands.AddToWishlist;

public record AddToWishlistCommand(Guid UserId, Guid ProductId) : IRequest<ServiceResult>;