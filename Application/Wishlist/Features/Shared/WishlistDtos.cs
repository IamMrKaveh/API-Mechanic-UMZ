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