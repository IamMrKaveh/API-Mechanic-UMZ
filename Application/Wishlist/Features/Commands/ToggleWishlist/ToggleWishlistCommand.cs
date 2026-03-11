using Application.Common.Models;

namespace Application.Wishlist.Features.Commands.ToggleWishlist;

public record ToggleWishlistCommand(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;