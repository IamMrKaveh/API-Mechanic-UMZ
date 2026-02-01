namespace Application.Services.Admin.Product;

public class AdminProductVariantShippingService : IAdminProductVariantShippingService
{
    private readonly IProductRepository _productRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IAppLogger<AdminProductVariantShippingService> _logger;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;

    public AdminProductVariantShippingService(
        IProductRepository productRepository,
        IShippingMethodRepository shippingMethodRepository,
        IAppLogger<AdminProductVariantShippingService> logger,
        IAuditService auditService,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _logger = logger;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<ProductVariantShippingInfoDto>> GetShippingMethodsAsync(int variantId)
    {
        try
        {
            var variant = await _productRepository.GetVariantWithDetailsAsync(variantId);

            if (variant == null)
            {
                return ServiceResult<ProductVariantShippingInfoDto>.Fail("محصول یافت نشد.");
            }

            var allShippingMethods = await _shippingMethodRepository.GetAllActiveAsync();

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
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            var variant = await _productRepository.GetVariantWithDetailsAsync(variantId);

            if (variant == null)
            {
                return ServiceResult.Fail("محصول یافت نشد.");
            }

            var validShippingMethodIds = await _shippingMethodRepository.GetAllActiveAsync();
            var validIds = validShippingMethodIds.Select(sm => sm.Id).ToHashSet();

            var invalidIds = dto.EnabledShippingMethodIds.Where(id => !validIds.Contains(id)).ToList();
            if (invalidIds.Any())
            {
                return ServiceResult.Fail($"برخی از روش‌های ارسال انتخاب شده نامعتبر هستند: {string.Join(", ", invalidIds)}");
            }

            variant.ShippingMultiplier = dto.ShippingMultiplier;

            // Update collection
            var existingMethods = variant.ProductVariantShippingMethods.ToList();
            var methodsToRemove = existingMethods.Where(x => !dto.EnabledShippingMethodIds.Contains(x.ShippingMethodId)).ToList();

            foreach (var item in methodsToRemove)
            {
                variant.ProductVariantShippingMethods.Remove(item);
            }

            var existingIds = existingMethods.Select(x => x.ShippingMethodId).ToHashSet();
            var idsToAdd = dto.EnabledShippingMethodIds.Where(id => !existingIds.Contains(id)).ToList();

            foreach (var methodId in idsToAdd)
            {
                variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod
                {
                    ShippingMethodId = methodId,
                    IsActive = true
                });
            }

            _productRepository.UpdateVariant(variant);
            await _unitOfWork.SaveChangesAsync();

            await _auditService.LogProductEventAsync(variant.ProductId, "UpdateShippingMethods", $"Shipping methods updated for variant {variantId} by user {currentUserId}.", currentUserId);

            await transaction.CommitAsync();
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating shipping methods for variant {VariantId}", variantId);
            return ServiceResult.Fail("خطا در به‌روزرسانی اطلاعات.");
        }
    }

    public async Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetAllShippingMethodsAsync()
    {
        var methods = await _shippingMethodRepository.GetAllActiveAsync();

        var dtos = methods.Select(m => new ShippingMethodDto
        {
            Id = m.Id,
            Name = m.Name,
            Cost = m.Cost,
            Description = m.Description,
            EstimatedDeliveryTime = m.EstimatedDeliveryTime,
            IsActive = m.IsActive,
            IsDeleted = m.IsDeleted,
            RowVersion = m.RowVersion != null ? Convert.ToBase64String(m.RowVersion) : null
        });

        return ServiceResult<IEnumerable<ShippingMethodDto>>.Ok(dtos);
    }
}