namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsHandler
    : IRequestHandler<GetAvailableShippingsQuery, ServiceResult<IEnumerable<AvailableShippingDto>>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetAvailableShippingsHandler(IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingDto>>> Handle(
        GetAvailableShippingsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _shippingQueryService.GetAvailableShippingsForCartAsync(
            request.UserId, cancellationToken);

        return ServiceResult<IEnumerable<AvailableShippingDto>>.Success(result);
    }
}