using Application.Shipping.Contracts;
using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public class GetAvailableShippingsForVariantsHandler(IShippingQueryService shippingQueryService)
    : IRequestHandler<GetAvailableShippingsForVariantsQuery, ServiceResult<IReadOnlyList<AvailableShippingDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<AvailableShippingDto>>> Handle(
        GetAvailableShippingsForVariantsQuery request,
        CancellationToken ct)
    {
        var result = await shippingQueryService.GetAvailableShippingsForVariantsAsync(request.VariantIds, ct);
        return ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(result);
    }
}