using Application.Shipping.Features.Shared;

namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public class GetProductVariantShippingHandler(
    IVariantQueryService variantQueryService,
    ILogger<GetProductVariantShippingHandler> logger) : IRequestHandler<GetVariantShippingQuery, ServiceResult<ProductVariantShippingInfoDto>>
{
    private readonly IVariantQueryService _variantQueryService = variantQueryService;
    private readonly ILogger<GetProductVariantShippingHandler> _logger = logger;

    public async Task<ServiceResult<ProductVariantShippingInfoDto>> Handle(
        GetVariantShippingQuery request,
        CancellationToken ct)
    {
        try
        {
            var result = await _variantQueryService.GetVariantShippingInfoAsync(request.VariantId, ct);

            return result is null
                ? ServiceResult<ProductVariantShippingInfoDto>.NotFound("محصول یافت نشد.")
                : ServiceResult<ProductVariantShippingInfoDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال variant {VariantId}", request.VariantId);
            return ServiceResult<ProductVariantShippingInfoDto>.Unexpected("خطا در دریافت اطلاعات.");
        }
    }
}