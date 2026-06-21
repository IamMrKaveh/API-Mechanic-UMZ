using Application.Shipping.Features.Shared;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Queries.GetVariantShipping;

public sealed class GetVariantShippingHandler(
    IVariantQueryService variantQueryService)
    : IQueryHandler<GetVariantShippingQuery, VariantShippingInfoDto>
{
    public async Task<ServiceResult<VariantShippingInfoDto>> Handle(
        GetVariantShippingQuery request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);

        var shipping = await variantQueryService
            .GetVariantShippingInfoAsync(variantId, ct);

        return shipping is null
            ? ServiceResult<VariantShippingInfoDto>.NotFound("اطلاعات حمل و نقل تنوع یافت نشد.")
            : ServiceResult<VariantShippingInfoDto>.Success(shipping);
    }
}