namespace Infrastructure.Shipping.Services;

public class ShippingQueryService : IShippingQueryService
{
    private readonly Persistence.Context.DBContext _context;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<ShippingQueryService> _logger;

    public ShippingQueryService(
        Persistence.Context.DBContext context,
        ICartRepository cartRepository,
        ILogger<ShippingQueryService> logger)
    {
        _context = context;
        _cartRepository = cartRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AvailableShippingDto>> GetAvailableShippingsForCartAsync(
        int userId, CancellationToken ct = default)
    {
        var cart = await _cartRepository.GetByUserIdAsync(userId, ct);
        if (cart == null || !cart.CartItems.Any())
            return Enumerable.Empty<AvailableShippingDto>();

        var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippings)
            .AsNoTracking()
            .ToListAsync(ct);

        var orderSubtotal = 0m;
        var cartItemDetails = new List<(int VariantId, decimal ShippingMultiplier, int Quantity)>();

        foreach (var cartItem in cart.CartItems)
        {
            var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId);
            if (variant == null) continue;

            orderSubtotal += variant.SellingPrice.Amount * cartItem.Quantity;
            cartItemDetails.Add((variant.Id, variant.ShippingMultiplier, cartItem.Quantity));
        }

        var orderTotal = Money.FromDecimal(orderSubtotal);

        var allShippings = await _context.Shippings
            .Where(sm => sm.IsActive && !sm.IsDeleted)
            .OrderBy(sm => sm.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        var enabledIdSets = variants
            .Select(v => v.ProductVariantShippings
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingId)
                .ToHashSet())
            .ToList();

        HashSet<int>? commonIds = null;
        foreach (var IdSet in enabledIdSets)
        {
            if (!IdSet.Any()) continue;

            if (commonIds == null)
                commonIds = new HashSet<int>(IdSet);
            else
                commonIds.IntersectWith(IdSet);
        }

        var availables = commonIds != null
            ? allShippings.Where(sm => commonIds.Contains(sm.Id)).ToList()
            : allShippings;

        var result = new List<AvailableShippingDto>();

        foreach (var method in availables)
        {
            if (!method.IsAvailableForOrder(orderTotal))
                continue;

            var cost = method.CalculateCostForCart(
                orderTotal,
                cartItemDetails.Select(ci => (ci.VariantId, ci.ShippingMultiplier, ci.Quantity)));

            var totalMultiplier = CalculateTotalMultiplier(cartItemDetails);

            result.Add(new AvailableShippingDto
            {
                Id = method.Id,
                Name = method.Name,
                BaseCost = method.BaseCost.Amount,
                TotalMultiplier = totalMultiplier,
                FinalCost = cost.Amount,
                IsFreeShipping = cost.Amount == 0,
                Description = method.Description,
                EstimatedDeliveryTime = method.GetDeliveryTimeDisplay(),
                MinDeliveryDays = method.MinDeliveryDays,
                MaxDeliveryDays = method.MaxDeliveryDays
            });
        }

        return result;
    }

    public async Task<ShippingCostResultDto> CalculateShippingCostAsync(
        int userId, int shippingId, CancellationToken ct = default)
    {
        var shipping = await _context.Shippings
            .AsNoTracking()
            .FirstOrDefaultAsync(sm => sm.Id == shippingId && !sm.IsDeleted, ct);

        if (shipping == null)
            throw new DomainException("روش ارسال یافت نشد.");

        if (!shipping.IsActive)
            throw new DomainException("روش ارسال غیرفعال است.");

        var cart = await _cartRepository.GetByUserIdAsync(userId, ct);
        if (cart == null || !cart.CartItems.Any())
            throw new DomainException("سبد خرید خالی است.");

        var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.Product)
            .AsNoTracking()
            .ToListAsync(ct);

        var orderSubtotal = 0m;
        var cartItemDetails = new List<(int VariantId, decimal ShippingMultiplier, int Quantity)>();
        var itemDetailDtos = new List<ShippingCostItemDetailDto>();

        foreach (var cartItem in cart.CartItems)
        {
            var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId);
            if (variant == null) continue;

            orderSubtotal += variant.SellingPrice.Amount * cartItem.Quantity;
            cartItemDetails.Add((variant.Id, variant.ShippingMultiplier, cartItem.Quantity));

            itemDetailDtos.Add(new ShippingCostItemDetailDto
            {
                VariantId = variant.Id,
                ProductName = variant.Product?.Name.Value ?? "محصول",
                Quantity = cartItem.Quantity,
                ShippingMultiplier = variant.ShippingMultiplier
            });
        }

        var orderTotal = Money.FromDecimal(orderSubtotal);

        var (isValid, error) = shipping.ValidateForOrder(orderTotal);
        if (!isValid)
            throw new DomainException(error!);

        var cost = shipping.CalculateCostForCart(
            orderTotal,
            cartItemDetails.Select(ci => (ci.VariantId, ci.ShippingMultiplier, ci.Quantity)));

        var totalMultiplier = CalculateTotalMultiplier(cartItemDetails);
        var isFree = cost.Amount == 0;

        decimal? remainingForFree = null;
        if (shipping.IsFreeAboveAmount && shipping.FreeShippingThreshold.HasValue && !isFree)
        {
            remainingForFree = shipping.FreeShippingThreshold.Value - orderSubtotal;
            if (remainingForFree < 0) remainingForFree = 0;
        }

        return new ShippingCostResultDto
        {
            ShippingId = shipping.Id,
            ShippingName = shipping.Name,
            BaseCost = shipping.BaseCost.Amount,
            TotalMultiplier = totalMultiplier,
            FinalCost = cost.Amount,
            IsFreeShipping = isFree,
            OrderSubtotal = orderSubtotal,
            FreeShippingThreshold = shipping.FreeShippingThreshold,
            RemainingForFreeShipping = remainingForFree,
            EstimatedDeliveryTime = shipping.GetDeliveryTimeDisplay(),
            ItemDetails = itemDetailDtos
        };
    }

    private static decimal CalculateTotalMultiplier(
        List<(int VariantId, decimal ShippingMultiplier, int Quantity)> items)
    {
        if (!items.Any()) return 1m;

        var totalMultiplier = 0m;
        var totalQuantity = 0;

        foreach (var item in items)
        {
            totalMultiplier += item.ShippingMultiplier * item.Quantity;
            totalQuantity += item.Quantity;
        }

        return totalQuantity > 0
            ? Math.Round(totalMultiplier / totalQuantity, 4)
            : 1m;
    }

    public async Task<IEnumerable<ShippingDto>> GetActiveShippingsAsync(CancellationToken ct = default)
    {
        var s = await _context.Shippings
            .Where(m => m.IsActive && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);

        return s.Select(m => new ShippingDto
        {
            Id = m.Id,
            Name = m.Name,
            Cost = m.BaseCost.Amount,
            Description = m.Description,
            EstimatedDeliveryTime = m.EstimatedDeliveryTime,
            IsActive = m.IsActive
        });
    }

    public async Task<ShippingDto?> GetShippingByIdAsync(int id, CancellationToken ct = default)
    {
        var m = await _context.Shippings
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (m == null)
            return null;

        return new ShippingDto
        {
            Id = m.Id,
            Name = m.Name,
            Cost = m.BaseCost.Amount,
            Description = m.Description,
            EstimatedDeliveryTime = m.EstimatedDeliveryTime,
            IsActive = m.IsActive
        };
    }

    public async Task<IEnumerable<AvailableShippingDto>> GetAvailableShippingsForVariantsAsync(
        IReadOnlyCollection<int> variantIds,
        CancellationToken ct = default)
    {
        if (variantIds == null || variantIds.Count == 0)
            return Enumerable.Empty<AvailableShippingDto>();

        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.ProductVariantShippings)
            .AsNoTracking()
            .ToListAsync(ct);

        if (!variants.Any())
            return Enumerable.Empty<AvailableShippingDto>();

        var allShippings = await _context.Shippings
            .Where(sm => sm.IsActive && !sm.IsDeleted)
            .OrderBy(sm => sm.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        var enabledIdSets = variants
            .Select(v => v.ProductVariantShippings
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingId)
                .ToHashSet())
            .ToList();

        HashSet<int>? commonIds = null;

        foreach (var IdSet in enabledIdSets)
        {
            if (!IdSet.Any())
                continue;

            if (commonIds == null)
                commonIds = new HashSet<int>(IdSet);
            else
                commonIds.IntersectWith(IdSet);
        }

        var availables = commonIds != null
            ? allShippings.Where(sm => commonIds.Contains(sm.Id))
            : allShippings;

        return availables.Select(m => new AvailableShippingDto
        {
            Id = m.Id,
            Name = m.Name,
            BaseCost = m.BaseCost.Amount,
            TotalMultiplier = 1m,
            FinalCost = m.BaseCost.Amount,
            IsFreeShipping = m.BaseCost.Amount == 0,
            Description = m.Description,
            EstimatedDeliveryTime = m.GetDeliveryTimeDisplay(),
            MinDeliveryDays = m.MinDeliveryDays,
            MaxDeliveryDays = m.MaxDeliveryDays
        }).ToList();
    }
}