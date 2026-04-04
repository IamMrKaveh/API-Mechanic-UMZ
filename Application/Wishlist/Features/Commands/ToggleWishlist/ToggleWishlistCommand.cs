using Application.Common.Results;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public record ToggleWishlistCommand(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;