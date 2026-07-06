using Application.Shipping.Contracts;
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
        var safeAmount = request.OrderAmount < 0 ? 0m : request.OrderAmount;
        var orderAmount = Money.Create(safeAmount);

        if (request.Items is null || request.Items.Count == 0)
        {
            var available = await shippingQueryService.GetAvailableShippingsAsync(orderAmount, ct);
            return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(available);
        }

        var result = await shippingQueryService.GetShippingQuotesAsync(orderAmount, request.Items, ct);

        if (result is null || result.Count == 0)
        {
            var available = await shippingQueryService.GetAvailableShippingsAsync(orderAmount, ct);
            return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(available);
        }

        return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(result);
    }
}