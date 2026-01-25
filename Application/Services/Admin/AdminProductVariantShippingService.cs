namespace Application.Services.Admin;

public class AdminProductVariantShippingService : IAdminProductVariantShippingService
{
    private readonly LedkaContext _context;
    private readonly ILogger<AdminProductVariantShippingService> _logger;
    private readonly IAuditService _auditService;

    public AdminProductVariantShippingService(
        LedkaContext context,
        ILogger<AdminProductVariantShippingService> logger,
        IAuditService auditService)
    {
        _context = context;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<ServiceResult<ProductVariantShippingInfoDto>> GetShippingMethodsAsync(int variantId)
    {
        try
        {
            var variant = await _context.ProductVariants
                .AsNoTracking()
                .Include(v => v.Product)
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                .Include(v => v.ProductVariantShippingMethods)
                    .ThenInclude(pvsm => pvsm.ShippingMethod)
                .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted);

            if (variant == null)
            {
                return ServiceResult<ProductVariantShippingInfoDto>.Fail("محصول یافت نشد.");
            }

            var allShippingMethods = await _context.ShippingMethods
                .AsNoTracking()
                .Where(sm => sm.IsActive && !sm.IsDeleted)
                .ToListAsync();

            var enabledMethodIds = variant.ProductVariantShippingMethods
                .Where(pvsm => pvsm.IsActive)
                .Select(pvsm => pvsm.ShippingMethodId)
                .ToHashSet();

            var result = new ProductVariantShippingInfoDto
            {
                VariantId = variant.Id,
                ProductName = variant.Product.Name,
                VariantDisplayName = variant.DisplayName,
                ShippingMultiplier = variant.ShippingMultiplier,
                AvailableShippingMethods = allShippingMethods.Select(sm => new ShippingMethodSelectionDto
                {
                    ShippingMethodId = sm.Id,
                    Name = sm.Name,
                    BaseCost = sm.Cost,
                    Description = sm.Description,
                    IsEnabled = enabledMethodIds.Contains(sm.Id)
                }).ToList()
            };

            return ServiceResult<ProductVariantShippingInfoDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال variant {VariantId}", variantId);
            return ServiceResult<ProductVariantShippingInfoDto>.Fail("خطا در دریافت اطلاعات.");
        }
    }

    public async Task<ServiceResult> UpdateShippingMethodsAsync(
        int variantId,
        UpdateProductVariantShippingMethodsDto dto,
        int currentUserId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var variant = await _context.ProductVariants
                .Include(v => v.ProductVariantShippingMethods)
                .FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted);

            if (variant == null)
            {
                return ServiceResult.Fail("محصول یافت نشد.");
            }

            var validShippingMethodIds = await _context.ShippingMethods
                .Where(sm => dto.EnabledShippingMethodIds.Contains(sm.Id) && sm.IsActive && !sm.IsDeleted)
                .Select(sm => sm.Id)
                .ToListAsync();

            if (validShippingMethodIds.Count != dto.EnabledShippingMethodIds.Count)
            {
                return ServiceResult.Fail("برخی از روش‌های ارسال انتخابی نامعتبر هستند.");
            }

            var oldMultiplier = variant.ShippingMultiplier;
            variant.ShippingMultiplier = dto.ShippingMultiplier;
            variant.UpdatedAt = DateTime.UtcNow;

            _context.ProductVariantShippingMethods.RemoveRange(variant.ProductVariantShippingMethods);

            foreach (var shippingMethodId in dto.EnabledShippingMethodIds)
            {
                variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod
                {
                    ProductVariantId = variantId,
                    ShippingMethodId = shippingMethodId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogAdminEventAsync(
                "Update ProductVariantShipping",
                currentUserId,
                $"Updated shipping methods. Multiplier: {oldMultiplier} → {dto.ShippingMultiplier}. " +
                $"Methods: {string.Join(", ", dto.EnabledShippingMethodIds)}");

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "خطا در بروزرسانی روش‌های ارسال variant {VariantId}", variantId);
            return ServiceResult.Fail("خطا در بروزرسانی اطلاعات.");
        }
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetAllShippingMethodsAsync()
    {
        try
        {
            var methods = await _context.ShippingMethods
                .AsNoTracking()
                .Where(sm => sm.IsActive && !sm.IsDeleted)
                .Select(sm => new ShippingMethodDto
                {
                    Id = sm.Id,
                    Name = sm.Name,
                    Cost = sm.Cost,
                    Description = sm.Description,
                    EstimatedDeliveryTime = sm.EstimatedDeliveryTime,
                    IsActive = sm.IsActive
                })
                .ToListAsync();

            return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(methods);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال");
            return ServiceResult<IEnumerable<ShippingMethodDto>>.Fail("خطا در دریافت اطلاعات.");
        }
    }
}