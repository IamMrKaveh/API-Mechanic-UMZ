namespace Application.Services;

public class CheckoutShippingService : ICheckoutShippingService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ILogger<CheckoutShippingService> _logger;

    public CheckoutShippingService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        ILogger<CheckoutShippingService> logger)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForCartAsync(int userId)
    {
        try
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || !cart.CartItems.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail("سبد خرید خالی است.");
            }

            var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();
            return await GetAvailableShippingMethodsForVariantsAsync(variantIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال برای سبد خرید کاربر {UserId}", userId);
            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                "خطایی در دریافت روش‌های ارسال رخ داد.  لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForVariantsAsync(IEnumerable<int> variantIds)
    {
        try
        {
            var variantIdList = variantIds.ToList();
            if (!variantIdList.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail("لیست محصولات خالی است.");
            }

            var variantsWithShipping = await _orderRepository.GetVariantsWithShippingMethodsAsync(variantIdList);

            if (!variantsWithShipping.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail("محصولات یافت نشدند.");
            }

            var variantsWithoutShipping = variantsWithShipping
                .Where(v => !v.ProductVariantShippingMethods.Any(s => s.IsActive))
                .ToList();

            if (variantsWithoutShipping.Any())
            {
                var productNames = string.Join(", ", variantsWithoutShipping.Select(v => v.Product.Name));
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                    $"برای محصولات زیر هیچ روش ارسالی تعریف نشده است: {productNames}");
            }

            var commonShippingMethods = variantsWithShipping
                .First()
                .ProductVariantShippingMethods
                .Where(s => s.IsActive)
                .Select(s => s.ShippingMethodId)
                .ToHashSet();

            foreach (var variant in variantsWithShipping.Skip(1))
            {
                var variantShippingMethodIds = variant.ProductVariantShippingMethods
                    .Where(s => s.IsActive)
                    .Select(s => s.ShippingMethodId)
                    .ToHashSet();

                commonShippingMethods.IntersectWith(variantShippingMethodIds);
            }

            if (!commonShippingMethods.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                    "هیچ روش ارسال مشترکی بین محصولات سبد خرید شما وجود ندارد.  لطفاً با پشتیبانی تماس بگیرید.");
            }

            var totalMultiplier = variantsWithShipping.Sum(v => v.ShippingMultiplier);

            var shippingMethods = await _orderRepository.GetShippingMethodsByIdsAsync(commonShippingMethods.ToList());

            var result = shippingMethods
                .Where(sm => sm.IsActive && !sm.IsDeleted)
                .Select(sm => new AvailableShippingMethodDto
                {
                    Id = sm.Id,
                    Name = sm.Name,
                    BaseCost = sm.Cost,
                    TotalMultiplier = totalMultiplier,
                    FinalCost = sm.Cost * totalMultiplier,
                    Description = sm.Description,
                    EstimatedDeliveryTime = sm.EstimatedDeliveryTime
                })
                .OrderBy(sm => sm.FinalCost)
                .ToList();

            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال برای واریانت‌ها");
            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                "خطایی در دریافت روش‌های ارسال رخ داد. لطفاً دوباره تلاش کنید.");
        }
    }
}