using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Queries.GetShipping;

public class GetShippingHandler(
    IShippingQueryService shippingQueryService) : IRequestHandler<GetShippingQuery, ServiceResult<ShippingDto>>
{
    public async Task<ServiceResult<ShippingDto>> Handle(
        GetShippingQuery request,
        CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.Id);

        var shipping = await shippingQueryService.GetShippingDetailAsync(shippingId, ct);

        return shipping is null
            ? ServiceResult<ShippingDto>.NotFound("روش ارسال یافت نشد.")
            : ServiceResult<ShippingDto>.Success(shipping);
    }
}