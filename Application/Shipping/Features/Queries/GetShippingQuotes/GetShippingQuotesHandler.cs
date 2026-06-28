using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippingQuotes;

public class GetShippingQuotesHandler(
    IShippingQueryService shippingQueryService)
    : IQueryHandler<GetShippingQuotesQuery, IReadOnlyList<AvailableShippingDto>>
{
    public async Task<ServiceResult<IReadOnlyList<AvailableShippingDto>>> Handle(
        GetShippingQuotesQuery request,
        CancellationToken ct)
    {
        var orderAmount = Money.Create(request.OrderAmount);
        var result = await shippingQueryService.GetShippingQuotesAsync(
            orderAmount,
            request.Items,
            ct);
        return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(result);
    }
}