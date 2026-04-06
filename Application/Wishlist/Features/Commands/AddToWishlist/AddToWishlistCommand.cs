using Application.Common.Results;

namespace Application.Wishlist.Features.Commands.AddToWishlist;

public record AddToWishlistCommand(int UserId, int ProductId) : IRequest<ServiceResult>;