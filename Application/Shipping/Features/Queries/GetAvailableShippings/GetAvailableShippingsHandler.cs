namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsHandler
    : IRequestHandler<GetAvailableShippingsQuery, ServiceResult<IEnumerable<AvailableShippingMethodDto>>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetAvailableShippingsHandler(IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> Handle(
        GetAvailableShippingsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _shippingQueryService.GetAvailableShippingMethodsForCartAsync(
            request.UserId, cancellationToken);

        return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Success(result);
    }
}