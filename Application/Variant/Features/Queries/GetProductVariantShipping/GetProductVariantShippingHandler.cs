namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public class GetProductVariantShippingHandler : IRequestHandler<GetProductVariantShippingQuery, ServiceResult<ProductVariantShippingInfoDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly ILogger<GetProductVariantShippingHandler> _logger;

    public GetProductVariantShippingHandler(
        IProductRepository productRepository,
        IShippingRepository shippingRepository,
        ILogger<GetProductVariantShippingHandler> logger)
    {
        _productRepository = productRepository;
        _shippingRepository = shippingRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<ProductVariantShippingInfoDto>> Handle(GetProductVariantShippingQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var variant = await _productRepository.GetVariantByIdAsync(request.VariantId);

            if (variant == null)
            {
                return ServiceResult<ProductVariantShippingInfoDto>.Failure("محصول یافت نشد.");
            }

            var allShippings = await _shippingRepository.GetAllAsync(false);

            var enabledIds = variant.ProductVariantShippings
                            .Where(pvsm => pvsm.IsActive)
                            .Select(pvsm => pvsm.ShippingId)
                            .ToHashSet();

            var result = new ProductVariantShippingInfoDto
            {
                VariantId = variant.Id, // Assuming ID is directly accessible or via DTO mapping logic
                ProductName = variant.Product?.Name,
                VariantDisplayName = variant.Sku ?? "N/A", // Simplified for example
                ShippingMultiplier = variant.ShippingMultiplier,
                AvailableShippings = allShippings.Select(sm => new ShippingSelectionDto
                {
                    ShippingId = sm.Id,
                    Name = sm.Name,
                    BaseCost = sm.Cost,
                    Description = sm.Description,
                    IsEnabled = enabledIds.Contains(sm.Id)
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