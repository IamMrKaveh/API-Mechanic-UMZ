namespace Application.User.Features.Shared;

public record WishlistDto(int Id, int ProductId, string ProductName, string ProductImage, decimal Price, bool IsInStock);

public record ToggleWishlistDto(int ProductId);