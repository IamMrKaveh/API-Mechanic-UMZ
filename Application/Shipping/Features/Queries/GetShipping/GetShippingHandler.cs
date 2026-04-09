using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShipping;

public class GetShippingHandler(
    IShippingQueryService shippingQueryService) : IRequestHandler<GetShippingQuery, ServiceResult<ShippingDto>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<ShippingDto>> Handle(
        GetShippingQuery request,
        CancellationToken ct)
    {
        var shipping = await _shippingQueryService.GetShippingDetailAsync(request.Id, ct);
        return shipping is null
            ? ServiceResult<ShippingDto>.NotFound("روش ارسال یافت نشد.")
            : ServiceResult<ShippingDto>.Success(shipping);
    }
}