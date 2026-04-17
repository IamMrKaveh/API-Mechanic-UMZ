using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Wishlist.QueryServices;

public sealed class WishlistQueryService(DBContext context) : IWishlistQueryService
{
    public async Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Join(context.Products.AsNoTracking(),
                w => w.ProductId,
                p => p.Id,
                (w, p) => new WishlistItemDto
                {
                    Id = w.Id.Value,
                    ProductId = p.Id.Value,
                    ProductName = p.Name.Value,
                    Slug = p.Slug != null ? p.Slug.Value : string.Empty,
                    AddedAt = w.CreatedAt
                })
            .ToListAsync(ct);

        return PaginatedResult<WishlistItemDto>.Create(items, total, page, pageSize);
    }

    public Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(UserId userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsInWishlistAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
        => await context.Wishlists.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);
}