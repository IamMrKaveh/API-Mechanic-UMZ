using Application.Shipping.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Queries.GetProductVariantShipping;

public class GetProductVariantShippingHandler(
    IVariantQueryService variantQueryService,
    ILogger<GetProductVariantShippingHandler> logger) : IRequestHandler<GetVariantShippingQuery, ServiceResult<ProductVariantShippingInfoDto>>
{
    public async Task<ServiceResult<ProductVariantShippingInfoDto>> Handle(
        GetVariantShippingQuery request,
        CancellationToken ct)
    {
        try
        {
            var variantId = VariantId.From(request.VariantId);

            var result = await variantQueryService.GetVariantShippingInfoAsync(variantId, ct);

            return result is null
                ? ServiceResult<ProductVariantShippingInfoDto>.NotFound("محصول یافت نشد.")
                : ServiceResult<ProductVariantShippingInfoDto>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "خطا در دریافت روش‌های ارسال variant {VariantId}", request.VariantId);
            return ServiceResult<ProductVariantShippingInfoDto>.Failure("خطا در دریافت اطلاعات.");
        }
    }
}