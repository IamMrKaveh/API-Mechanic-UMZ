using Application.Wishlist.Contracts;
using Infrastructure.Persistence.Context;
using SharedKernel.Models;

namespace Infrastructure.Wishlist.QueryServices;

public class WishlistQueryService(DBContext context) : IWishlistQueryService
{
    private readonly DBContext _context = context;

    public async Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(w => new WishlistItemDto
            {
                Id = w.Id,
                ProductId = w.ProductId,
                ProductName = w.Product.Name.Value,
                MinPrice = w.Product.Stats.MinPrice.Amount,
                IsInStock = w.Product.Stats.TotalStock > 0 || w.Product.Variants.Any(v => v.IsUnlimited),
                IconUrl = null,
                AddedAt = w.CreatedAt
            })
            .ToListAsync(ct);

        return PaginatedResult<WishlistItemDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<bool> IsInWishlistAsync(
        int userId,
        int productId,
        CancellationToken ct = default)
    {
        return await _context.Wishlists
            .AsNoTracking()
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, ct);
    }
}