using Application.Wishlist.Features.Queries.GetWishlistById;
using SharedKernel.Models;

namespace Application.Wishlist.Contracts;

public interface IWishlistQueryService
{
    Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<bool> IsInWishlistAsync(
        int userId,
        int productId,
        CancellationToken ct = default);
}