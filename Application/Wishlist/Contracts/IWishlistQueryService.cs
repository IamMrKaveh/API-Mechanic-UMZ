using Application.Wishlist.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Wishlist.Contracts;

public interface IWishlistQueryService
{
    Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> IsInWishlistAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default);
}