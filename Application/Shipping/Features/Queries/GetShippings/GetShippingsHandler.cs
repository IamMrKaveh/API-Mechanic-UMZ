using Application.Common.Results;
using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandler(
    IShippingQueryService shippingQueryService) : IRequestHandler<GetShippingsQuery, ServiceResult<IReadOnlyList<ShippingListItemDto>>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<IReadOnlyList<ShippingListItemDto>>> Handle(
        GetShippingsQuery request,
        CancellationToken ct)
    {
        var shippings = await _shippingQueryService.GetAllShippingsAsync(request.IncludeInactive, ct);
        return ServiceResult<IReadOnlyList<ShippingListItemDto>>.Success(shippings);
    }
}