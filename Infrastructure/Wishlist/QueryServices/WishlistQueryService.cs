using Application.Wishlist.Contracts;
using Application.Wishlist.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Wishlist.QueryServices;

public sealed class WishlistQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IWishlistQueryService
{
    private const int DefaultPageSize = 10;
    private const string ProductEntityType = "Product";

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

        var rows = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((effectivePage - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .Join(context.Products.AsNoTracking(),
                w => w.ProductId,
                p => p.Id,
                (w, p) => new
                {
                    WishlistId = w.Id.Value,
                    ProductId = p.Id.Value,
                    ProductName = p.Name,
                    AddedAt = w.CreatedAt,
                    IconPath = context.Medias
                        .AsNoTracking()
                        .Where(m => m.EntityType == ProductEntityType
                                    && m.EntityId == p.Id
                                    && m.IsPrimary
                                    && m.IsActive)
                        .Select(m => m.FilePath)
                        .FirstOrDefault()
                })
            .ToListAsync(ct);

        var dtos = rows.Select(x => new WishlistItemDto(
            Id: x.WishlistId,
            ProductId: x.ProductId,
            ProductName: x.ProductName,
            MinPrice: 0m,
            IsInStock: false,
            IconUrl: !string.IsNullOrWhiteSpace(x.IconPath)
                ? urlResolver.ResolveMediaUrl(x.IconPath)
                : null,
            AddedAt: x.AddedAt
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