using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public class GetProductVariantShippingHandler : IRequestHandler<GetProductVariantShippingQuery, ServiceResult<ProductVariantShippingInfoDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly ILogger<GetProductVariantShippingHandler> _logger;

    public GetProductVariantShippingHandler(
        IProductRepository productRepository,
        IShippingRepository shippingMethodRepository,
        ILogger<GetProductVariantShippingHandler> logger)
    {
        _productRepository = productRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<ProductVariantShippingInfoDto>> Handle(GetProductVariantShippingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var variant = await _productRepository.GetVariantByIdAsync(request.VariantId); // Assuming extension/repo method exists for details

            if (variant == null)
            {
                return ServiceResult<ProductVariantShippingInfoDto>.Failure("محصول یافت نشد.");
            }

            var allShippingMethods = await _shippingMethodRepository.GetAllAsync(false);

            var enabledMethodIds = variant.ProductVariantShippingMethods
                            .Where(pvsm => pvsm.IsActive)
                            .Select(pvsm => pvsm.ShippingId)
                            .ToHashSet();

            var result = new ProductVariantShippingInfoDto
            {
                VariantId = variant.Id, // Assuming ID is directly accessible or via DTO mapping logic
                ProductName = variant.Product?.Name,
                VariantDisplayName = variant.Sku ?? "N/A", // Simplified for example
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

            return ServiceResult<ProductVariantShippingInfoDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال variant {VariantId}", request.VariantId);
            return ServiceResult<ProductVariantShippingInfoDto>.Failure("خطا در دریافت اطلاعات.");
        }
    }
}