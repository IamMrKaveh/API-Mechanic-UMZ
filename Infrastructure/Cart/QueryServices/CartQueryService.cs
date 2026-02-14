namespace Infrastructure.Cart.QueryService;

/// <summary>
/// سرویس کوئری سبد خرید - مستقیماً DTO برمی‌گرداند.
/// بدون بارگذاری Aggregate - بهینه برای خواندن.
/// </summary>
public class CartQueryService : ICartQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;
    private readonly ILogger<CartQueryService> _logger;

    public CartQueryService(
        LedkaContext context,
        IMediaService mediaService,
        ILogger<CartQueryService> logger)
    {
        _context = context;
        _mediaService = mediaService;
        _logger = logger;
    }

    public async Task<CartDetailDto?> GetCartDetailAsync(
        int? userId, string? guestToken, CancellationToken ct = default)
    {
        var cartQuery = BuildCartQuery(userId, guestToken);

        var cart = await cartQuery
            .Select(c => new
            {
                c.Id,
                c.UserId,
                c.GuestToken,
                Items = c.CartItems.Select(ci => new
                {
                    ci.VariantId,
                    ci.Quantity,
                    ci.SellingPrice,
                    VariantSku = ci.Variant!.Sku,
                    VariantIsActive = ci.Variant.IsActive,
                    VariantIsDeleted = ci.Variant.IsDeleted,
                    VariantSellingPrice = ci.Variant.SellingPrice,
                    VariantStock = ci.Variant.StockQuantity,
                    VariantIsUnlimited = ci.Variant.IsUnlimited,
                    ci.Variant.ProductId,
                    ProductName = ci.Variant.Product!.Name,
                    ProductIsActive = ci.Variant.Product.IsActive,
                    ProductIsDeleted = ci.Variant.Product.IsDeleted,
                    Attributes = ci.Variant.VariantAttributes
                        .Where(va => va.AttributeValue != null && va.AttributeValue.AttributeType != null)
                        .Select(va => new
                        {
                            Key = va.AttributeValue!.AttributeType.Name,
                            Value = va.AttributeValue.DisplayValue ?? va.AttributeValue.Value
                        })
                        .ToList()
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart == null) return null;

        var priceChanges = new List<CartPriceChangeDto>();
        var items = new List<CartItemDetailDto>();

        foreach (var item in cart.Items)
        {
            var isAvailable = item.VariantIsActive && !item.VariantIsDeleted
                && item.ProductIsActive && !item.ProductIsDeleted;

            // شناسایی تغییر قیمت
            if (item.SellingPrice != item.VariantSellingPrice && isAvailable)
            {
                priceChanges.Add(new CartPriceChangeDto(
                    item.VariantId,
                    item.ProductName,
                    item.SellingPrice,
                    item.VariantSellingPrice));
            }

            var productImage = await _mediaService.GetPrimaryImageUrlAsync("Product", item.ProductId);

            items.Add(new CartItemDetailDto
            {
                VariantId = item.VariantId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                VariantSku = item.VariantSku,
                ProductImage = productImage,
                Quantity = item.Quantity,
                UnitPrice = item.SellingPrice,
                CurrentPrice = item.VariantSellingPrice,
                TotalPrice = item.Quantity * item.SellingPrice,
                Stock = item.VariantStock,
                IsUnlimited = item.VariantIsUnlimited,
                IsAvailable = isAvailable,
                Attributes = item.Attributes.ToDictionary(a => a.Key, a => a.Value)
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
        int? userId, string? guestToken, CancellationToken ct = default)
    {
        var cartQuery = BuildCartQuery(userId, guestToken);

        var summary = await cartQuery
            .Select(c => new CartSummaryDto
            {
                ItemCount = c.CartItems.Count,
                TotalQuantity = c.CartItems.Sum(ci => ci.Quantity),
                TotalPrice = c.CartItems.Sum(ci => ci.Quantity * ci.SellingPrice)
            })
            .FirstOrDefaultAsync(ct);

        return summary ?? new CartSummaryDto
        {
            ItemCount = 0,
            TotalQuantity = 0,
            TotalPrice = 0
        };
    }

    public async Task<CartCheckoutValidationDto> ValidateCartForCheckoutAsync(
        int? userId, string? guestToken, CancellationToken ct = default)
    {
        var cartQuery = BuildCartQuery(userId, guestToken);

        var cart = await cartQuery
            .Select(c => new
            {
                c.Id,
                IsEmpty = !c.CartItems.Any(),
                Items = c.CartItems.Select(ci => new
                {
                    ci.VariantId,
                    ci.Quantity,
                    ci.SellingPrice,
                    VariantSellingPrice = ci.Variant!.SellingPrice,
                    VariantIsActive = ci.Variant.IsActive,
                    VariantIsDeleted = ci.Variant.IsDeleted,
                    VariantStock = ci.Variant.StockQuantity,
                    VariantIsUnlimited = ci.Variant.IsUnlimited,
                    ProductName = ci.Variant.Product!.Name,
                    ProductIsActive = ci.Variant.Product.IsActive,
                    ProductIsDeleted = ci.Variant.Product.IsDeleted
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (cart == null)
        {
            return new CartCheckoutValidationDto
            {
                IsValid = false,
                Errors = new List<string> { "سبد خرید یافت نشد." }
            };
        }

        if (cart.IsEmpty)
        {
            return new CartCheckoutValidationDto
            {
                IsValid = false,
                Errors = new List<string> { "سبد خرید خالی است." }
            };
        }

        var errors = new List<string>();
        var priceChanges = new List<CartPriceChangeDto>();
        var stockIssues = new List<CartStockIssueDto>();

        foreach (var item in cart.Items)
        {
            var isAvailable = item.VariantIsActive && !item.VariantIsDeleted
                && item.ProductIsActive && !item.ProductIsDeleted;

            if (!isAvailable)
            {
                errors.Add($"محصول «{item.ProductName}» دیگر در دسترس نیست.");
                continue;
            }

            if (!item.VariantIsUnlimited && item.VariantStock < item.Quantity)
            {
                stockIssues.Add(new CartStockIssueDto(
                    item.VariantId,
                    item.ProductName,
                    item.Quantity,
                    item.VariantStock));
            }

            if (item.SellingPrice != item.VariantSellingPrice)
            {
                priceChanges.Add(new CartPriceChangeDto(
                    item.VariantId,
                    item.ProductName,
                    item.SellingPrice,
                    item.VariantSellingPrice));
            }
        }

        var isValid = errors.Count == 0 && stockIssues.Count == 0;

        // اگر فقط تغییر قیمت داریم، سبد هنوز معتبر است ولی کاربر باید مطلع شود
        return new CartCheckoutValidationDto
        {
            IsValid = isValid,
            Errors = errors,
            PriceChanges = priceChanges,
            StockIssues = stockIssues
        };
    }

    private IQueryable<Domain.Cart.Cart> BuildCartQuery(int? userId, string? guestToken)
    {
        var query = _context.Carts.AsNoTracking().AsQueryable();

        if (userId.HasValue)
            return query.Where(c => c.UserId == userId.Value && !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(guestToken))
            return query.Where(c => c.GuestToken == guestToken && !c.IsDeleted);

        // هیچ‌کدام - سبد خالی
        return query.Where(c => false);
    }
}