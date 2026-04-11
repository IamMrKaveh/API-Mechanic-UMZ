using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandler(
    IShippingQueryService shippingQueryService) : IRequestHandler<GetShippingsQuery, ServiceResult<IReadOnlyList<ShippingListItemDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<ShippingListItemDto>>> Handle(
        GetShippingsQuery request,
        CancellationToken ct)
    {
        var shippings = await shippingQueryService.GetAllShippingsAsync(request.IncludeInactive, ct);
        return ServiceResult<IReadOnlyList<ShippingListItemDto>>.Success(shippings);
    }
}