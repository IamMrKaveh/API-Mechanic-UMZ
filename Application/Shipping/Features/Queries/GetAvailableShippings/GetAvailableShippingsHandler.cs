using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsHandler(IShippingQueryService shippingQueryService)
    : IRequestHandler<GetAvailableShippingsQuery, ServiceResult<IReadOnlyList<AvailableShippingDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<AvailableShippingDto>>> Handle(
        GetAvailableShippingsQuery request,
        CancellationToken ct)
    {
        var orderAmount = Money.Create(request.OrderAmount);
        var result = await shippingQueryService.GetAvailableShippingsAsync(orderAmount, ct);
        return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(result);
    }
}