using Application.Shipping.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Queries.GetVariantShipping;

public class GetVariantShippingHandler(
    IVariantQueryService variantQueryService,
    IAuditService auditService) : IRequestHandler<GetVariantShippingQuery, ServiceResult<ProductVariantShippingInfoDto>>
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
                ? ServiceResult<ProductVariantShippingInfoDto>.NotFound("واریانت یافت نشد.")
                : ServiceResult<ProductVariantShippingInfoDto>.Success(result);
        }
        catch (Exception)
        {
            return ServiceResult<ProductVariantShippingInfoDto>.Failure("خطا در دریافت اطلاعات ارسال.");
        }
    }
}