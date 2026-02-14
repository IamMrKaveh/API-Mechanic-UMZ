namespace Application.Order.Features.Queries.GetAvailableShippingMethods;

public class GetAvailableShippingMethodsHandler
    : IRequestHandler<GetAvailableShippingMethodsQuery, ServiceResult<IEnumerable<AvailableShippingMethodDto>>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetAvailableShippingMethodsHandler(IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> Handle(
        GetAvailableShippingMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _shippingQueryService.GetAvailableShippingMethodsForCartAsync(
            request.UserId, cancellationToken);

        return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Success(result);
    }
}