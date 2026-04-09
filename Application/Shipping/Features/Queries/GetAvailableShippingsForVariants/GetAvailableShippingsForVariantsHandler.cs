using Application.Shipping.Contracts;

namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public class GetAvailableShippingsForVariantsHandler(
    IShippingQueryService shippingQueryService)
        : IRequestHandler<
        GetAvailableShippingsForVariantsQuery,
        ServiceResult<IEnumerable<AvailableShippingDto>>>
{
    private readonly IShippingQueryService _shippingQueryService = shippingQueryService;

    public async Task<ServiceResult<IEnumerable<AvailableShippingDto>>> Handle(
        GetAvailableShippingsForVariantsQuery request,
        CancellationToken ct)
    {
        if (request.VariantIds == null || request.VariantIds.Count == 0)
            return ServiceResult<IEnumerable<AvailableShippingDto>>
                .Success([]);

        var result = await _shippingQueryService
            .GetAvailableShippingsForVariantsAsync(
                request.VariantIds,
                ct);

        return ServiceResult<IEnumerable<AvailableShippingDto>>
            .Success(result);
    }
}