using Domain.User.ValueObjects;

namespace Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsHandler(IShippingQueryService shippingQueryService)
        : IRequestHandler<GetAvailableShippingsQuery, ServiceResult<IEnumerable<AvailableShippingDto>>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<IEnumerable<AvailableShippingDto>>> Handle(
        GetAvailableShippingsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        var result = await shippingQueryService.GetAvailableShippingsForCartAsync(
            userId,
            ct);

        return ServiceResult<IEnumerable<AvailableShippingDto>>.Success(result);
    }
}