using Application.Wishlist.Features.Shared;
using SharedKernel.Models;

namespace Application.Wishlist.Contracts;

public interface IWishlistQueryService
{
    Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> IsInWishlistAsync(
        Guid userId,
        int productId,
        CancellationToken ct = default);
}