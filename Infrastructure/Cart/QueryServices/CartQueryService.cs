using Application.Cart.Contracts;
using Application.Cart.Features.Shared;
using Domain.Cart.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Cart.QueryServices;

public sealed class CartQueryService(
    DBContext context,
    IMediaQueryService mediaService) : ICartQueryService
{
    public async Task<CartDetailDto?> GetCartDetailAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default)
    {
        var query = BuildBaseCartQuery(userId, guestToken);

        var cart = await query
            .AsNoTracking()
            .Select(c => new
            {
                Id = c.Id.Value,
                UserId = c.UserId != null ? c.UserId.Value : (Guid?)null,
                GuestToken = c.GuestToken != null ? c.GuestToken.Value : null,
                Items = c.CartItems.Select(ci => new
                {
                    VariantId = ci.VariantId.Value,
                    ProductId = ci.ProductId.Value,
                    ProductName = ci.ProductName.Value,
                    VariantSku = ci.Sku.Value,
                    ci.Quantity,
                    UnitPrice = ci.SellingPrice.Amount,
                    Currency = ci.SellingPrice.Currency
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart is null) return null;

        var items = new List<CartItemDetailDto>();
        var priceChanges = new List<CartPriceChangeDto>();

        foreach (var item in cart.Items)
        {
            var primaryMedia = await mediaService.GetPrimaryByEntityAsync(
                "Product", item.ProductId, ct);

            items.Add(new CartItemDetailDto
            {
                VariantId = item.VariantId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                VariantSku = item.VariantSku,
                ProductImage = primaryMedia?.PublicUrl,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                CurrentPrice = item.UnitPrice,
                TotalPrice = item.Quantity * item.UnitPrice,
                Stock = 0,
                IsUnlimited = false,
                IsAvailable = true
            });
        }

        return new CartDetailDto
        {
            Id = cart.Id,
            UserId = cart.UserId,
            GuestToken = cart.GuestToken,
            Items = items,
            TotalPrice = items.Sum(i => i.TotalPrice),
            TotalItems = items.Sum(i => i.Quantity),
            PriceChanges = priceChanges
        };
    }

    public async Task<CartSummaryDto> GetCartSummaryAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default)
    {
        var query = BuildBaseCartQuery(userId, guestToken);

        var result = await query
            .AsNoTracking()
            .Select(c => new CartSummaryDto
            {
                ItemCount = c.CartItems.Count,
                TotalQuantity = c.CartItems.Sum(ci => ci.Quantity),
                TotalPrice = c.CartItems.Sum(ci => ci.Quantity * ci.SellingPrice.Amount)
            })
            .FirstOrDefaultAsync(ct);

        return result ?? new CartSummaryDto
        {
            ItemCount = 0,
            TotalQuantity = 0,
            TotalPrice = 0
        };
    }

    public async Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(
        UserId? userId,
        GuestToken? guestToken,
        CancellationToken ct = default)
    {
        var query = BuildBaseCartQuery(userId, guestToken);

        var cart = await query
            .AsNoTracking()
            .Select(c => new
            {
                IsEmpty = !c.CartItems.Any(),
                Items = c.CartItems.Select(ci => new
                {
                    VariantId = ci.VariantId.Value,
                    ProductName = ci.ProductName.Value,
                    ci.Quantity,
                    UnitPrice = ci.SellingPrice.Amount
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart is null)
        {
            return new CartCheckoutValidationDto
            {
                IsValid = false,
                Errors = ["سبد خرید یافت نشد."]
            };
        }

        if (cart.IsEmpty)
        {
            return new CartCheckoutValidationDto
            {
                IsValid = false,
                Errors = ["سبد خرید خالی است."]
            };
        }

        return new CartCheckoutValidationDto { IsValid = true };
    }

    private IQueryable<Domain.Cart.Aggregates.Cart> BuildBaseCartQuery(
        UserId? userId,
        GuestToken? guestToken)
    {
        if (userId is not null)
        {
            return context.Carts
                .Include(c => c.CartItems)
                .Where(c => c.UserId == userId && !c.IsCheckedOut);
        }

        if (guestToken is not null)
        {
            return context.Carts
                .Include(c => c.CartItems)
                .Where(c => c.GuestToken == guestToken && !c.IsCheckedOut);
        }

        return context.Carts.Where(_ => false);
    }
}