using Application.Common.Results;
using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippingById;

public class GetShippingByIdHandler(IShippingQueryService shippingQueryService) : IRequestHandler<GetShippingByIdQuery, ServiceResult<ShippingDto>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<ShippingDto>> Handle(
        GetShippingByIdQuery request,
        CancellationToken ct)
    {
        var shipping = await _shippingQueryService.GetShippingByIdAsync(request.Id, ct);
        if (shipping is null)
            return ServiceResult<ShippingDto>.NotFound("NotFound");
        return ServiceResult<ShippingDto>.Success(shipping);
    }
}