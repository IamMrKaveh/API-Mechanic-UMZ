using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostHandler(IShippingQueryService shippingQueryService)
        : IRequestHandler<CalculateShippingCostQuery, ServiceResult<ShippingCostResultDto>>
{
    public async Task<ServiceResult<ShippingCostResultDto>> Handle(
        CalculateShippingCostQuery request,
        CancellationToken ct)
    {
        var shippingId = ShippingId.From(request.ShippingId);
        var userId = UserId.From(request.UserId);

        var result = await shippingQueryService.CalculateShippingCostAsync(
            userId,
            shippingId,
            ct);

        return ServiceResult<ShippingCostResultDto>.Success(result);
    }
}