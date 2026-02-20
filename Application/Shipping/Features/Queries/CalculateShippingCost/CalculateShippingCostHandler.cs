namespace Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostHandler
    : IRequestHandler<CalculateShippingCostQuery, ServiceResult<ShippingCostResultDto>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public CalculateShippingCostHandler(IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<ShippingCostResultDto>> Handle(
        CalculateShippingCostQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _shippingQueryService.CalculateShippingCostAsync(
            request.UserId, request.ShippingMethodId, cancellationToken);

        return ServiceResult<ShippingCostResultDto>.Success(result);
    }
}