namespace Application.User.Features.Commands.ToggleWishlist;

public record ToggleWishlistCommand(int UserId, int ProductId) : IRequest<ServiceResult<bool>>;