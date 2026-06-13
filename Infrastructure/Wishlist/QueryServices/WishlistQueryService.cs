using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Wishlist.QueryServices;

public sealed class WishlistQueryService(DBContext context) : IWishlistQueryService
{
    private const int DefaultPageSize = 10;

    public async Task<PaginatedResult<WishlistItemDto>> GetPagedAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var effectivePage = page > 0 ? page : 1;
        var effectivePageSize = pageSize > 0 ? pageSize : DefaultPageSize;

        var query = context.Wishlists
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Join(context.Products.AsNoTracking(),
                w => w.ProductId,
                p => p.Id,
                (w, p) => new { w, p })
            .ToListAsync(ct);

        var dtos = items.Select(x => new WishlistItemDto(
            Id: x.w.Id.Value,
            ProductId: x.p.Id.Value,
            ProductName: x.p.Name,
            MinPrice: 0m,
            IsInStock: false,
            IconUrl: null,
            AddedAt: x.w.CreatedAt
        )).ToList();

        return PaginatedResult<WishlistItemDto>.Create(dtos, total, effectivePage, effectivePageSize);
    }

    public async Task<bool> IsInWishlistAsync(
        UserId userId,
        ProductId productId,
        CancellationToken ct = default)
        => await context.Wishlists.AnyAsync(
            w => w.UserId == userId && w.ProductId == productId, ct);
}