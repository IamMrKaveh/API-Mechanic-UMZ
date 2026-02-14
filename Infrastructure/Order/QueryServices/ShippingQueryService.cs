namespace Infrastructure.Order.QueryServices;

public class ShippingQueryService : IShippingQueryService
{
    private readonly LedkaContext _context;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<ShippingQueryService> _logger;

    public ShippingQueryService(
        LedkaContext context,
        ICartRepository cartRepository,
        ILogger<ShippingQueryService> logger)
    {
        _context = context;
        _cartRepository = cartRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AvailableShippingMethodDto>> GetAvailableShippingMethodsForCartAsync(
        int userId, CancellationToken ct = default)
    {
        // 1. Load cart with variant details
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null || !cart.CartItems.Any())
            return Enumerable.Empty<AvailableShippingMethodDto>();

        var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

        // 2. Load variants with shipping method associations
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippingMethods)
            .AsNoTracking()
            .ToListAsync(ct);

        // 3. Calculate order subtotal
        var orderSubtotal = 0m;
        var cartItemDetails = new List<(int VariantId, decimal ShippingMultiplier, int Quantity)>();

        foreach (var cartItem in cart.CartItems)
        {
            var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId);
            if (variant == null) continue;

            orderSubtotal += variant.SellingPrice * cartItem.Quantity;
            cartItemDetails.Add((variant.Id, variant.ShippingMultiplier, cartItem.Quantity));
        }

        var orderTotal = Money.FromDecimal(orderSubtotal);

        // 4. Load all active shipping methods
        var allShippingMethods = await _context.Set<ShippingMethod>()
            .Where(sm => sm.IsActive && !sm.IsDeleted)
            .OrderBy(sm => sm.SortOrder)
            .AsNoTracking()
            .ToListAsync(ct);

        // 5. Filter: only shipping methods that ALL cart variants support
        var enabledMethodIdSets = variants
            .Select(v => v.ProductVariantShippingMethods
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingMethodId)
                .ToHashSet())
            .ToList();

        // If any variant has no shipping methods configured, allow all methods (fallback)
        HashSet<int>? commonMethodIds = null;
        foreach (var methodIdSet in enabledMethodIdSets)
        {
            if (!methodIdSet.Any()) continue; // Skip variants with no specific shipping config

            if (commonMethodIds == null)
                commonMethodIds = new HashSet<int>(methodIdSet);
            else
                commonMethodIds.IntersectWith(methodIdSet);
        }

        var availableMethods = commonMethodIds != null
            ? allShippingMethods.Where(sm => commonMethodIds.Contains(sm.Id)).ToList()
            : allShippingMethods;

        // 6. Filter by order amount limits and calculate costs
        var result = new List<AvailableShippingMethodDto>();

        foreach (var method in availableMethods)
        {
            if (!method.IsAvailableForOrder(orderTotal))
                continue;

            var cost = method.CalculateCostForCart(
                orderTotal,
                cartItemDetails.Select(ci => (ci.VariantId, ci.ShippingMultiplier, ci.Quantity)));

            var totalMultiplier = CalculateTotalMultiplier(cartItemDetails);

            result.Add(new AvailableShippingMethodDto
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
        int userId, int shippingMethodId, CancellationToken ct = default)
    {
        // 1. Load shipping method
        var shippingMethod = await _context.Set<ShippingMethod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(sm => sm.Id == shippingMethodId && !sm.IsDeleted, ct);

        if (shippingMethod == null)
            throw new DomainException("روش ارسال یافت نشد.");

        if (!shippingMethod.IsActive)
            throw new DomainException("روش ارسال غیرفعال است.");

        // 2. Load cart
        var cart = await _cartRepository.GetByUserIdAsync(userId);
        if (cart == null || !cart.CartItems.Any())
            throw new DomainException("سبد خرید خالی است.");

        var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

        // 3. Load variants
        var variants = await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id) && !v.IsDeleted)
            .Include(v => v.Product)
            .AsNoTracking()
            .ToListAsync(ct);

        // 4. Build item details and calculate subtotal
        var orderSubtotal = 0m;
        var cartItemDetails = new List<(int VariantId, decimal ShippingMultiplier, int Quantity)>();
        var itemDetailDtos = new List<ShippingCostItemDetailDto>();

        foreach (var cartItem in cart.CartItems)
        {
            var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId);
            if (variant == null) continue;

            orderSubtotal += variant.SellingPrice * cartItem.Quantity;
            cartItemDetails.Add((variant.Id, variant.ShippingMultiplier, cartItem.Quantity));

            itemDetailDtos.Add(new ShippingCostItemDetailDto
            {
                VariantId = variant.Id,
                ProductName = variant.Product?.Name ?? "محصول",
                Quantity = cartItem.Quantity,
                ShippingMultiplier = variant.ShippingMultiplier
            });
        }

        var orderTotal = Money.FromDecimal(orderSubtotal);

        // 5. Validate availability
        var (isValid, error) = shippingMethod.ValidateForOrder(orderTotal);
        if (!isValid)
            throw new DomainException(error!);

        // 6. Calculate cost
        var cost = shippingMethod.CalculateCostForCart(
            orderTotal,
            cartItemDetails.Select(ci => (ci.VariantId, ci.ShippingMultiplier, ci.Quantity)));

        var totalMultiplier = CalculateTotalMultiplier(cartItemDetails);
        var isFree = cost.Amount == 0;

        decimal? remainingForFree = null;
        if (shippingMethod.IsFreeAboveAmount && shippingMethod.FreeShippingThreshold.HasValue && !isFree)
        {
            remainingForFree = shippingMethod.FreeShippingThreshold.Value - orderSubtotal;
            if (remainingForFree < 0) remainingForFree = 0;
        }

        return new ShippingCostResultDto
        {
            ShippingMethodId = shippingMethod.Id,
            ShippingMethodName = shippingMethod.Name,
            BaseCost = shippingMethod.BaseCost.Amount,
            TotalMultiplier = totalMultiplier,
            FinalCost = cost.Amount,
            IsFreeShipping = isFree,
            OrderSubtotal = orderSubtotal,
            FreeShippingThreshold = shippingMethod.FreeShippingThreshold,
            RemainingForFreeShipping = remainingForFree,
            EstimatedDeliveryTime = shippingMethod.GetDeliveryTimeDisplay(),
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

    public Task<IEnumerable<ShippingMethodDto>> GetActiveShippingMethodsAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ShippingMethodDto?> GetShippingMethodByIdAsync(int id, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}