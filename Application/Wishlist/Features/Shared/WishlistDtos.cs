namespace Application.Wishlist.Features.Shared;

public sealed record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal MinPrice,
    bool IsInStock,
    string? IconUrl,
    DateTime AddedAt
);

public record WishlistDto(Guid Id, Guid ProductId, string ProductName, string ProductImage, decimal Price, bool IsInStock);

public record ToggleWishlistDto(Guid ProductId);