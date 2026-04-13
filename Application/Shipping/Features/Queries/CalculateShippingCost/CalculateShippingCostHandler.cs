using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;

namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostHandler(IShippingQueryService shippingQueryService)
    : IRequestHandler<CalculateShippingCostQuery, ServiceResult<ShippingCostResultDto>>
{
    public async Task<ServiceResult<ShippingCostResultDto>> Handle(
        CalculateShippingCostQuery request,
        CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.ShippingId);
        var orderAmount = Money.Create(request.OrderAmount);

        var result = await shippingQueryService.CalculateShippingCostAsync(shippingId, orderAmount, ct);

        return ServiceResult<ShippingCostResultDto>.Success(result);
    }
}