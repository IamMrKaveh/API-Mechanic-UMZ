namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public class GetAvailableShippingsForVariantsHandler
    : IRequestHandler<
        GetAvailableShippingsForVariantsQuery,
        ServiceResult<IEnumerable<AvailableShippingMethodDto>>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetAvailableShippingsForVariantsHandler(
        IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> Handle(
        GetAvailableShippingsForVariantsQuery request,
        CancellationToken ct)
    {
        if (request.VariantIds == null || !request.VariantIds.Any())
            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>
                .Success(Array.Empty<AvailableShippingMethodDto>());

        var result = await _shippingQueryService
            .GetAvailableShippingMethodsForVariantsAsync(
                request.VariantIds,
                ct);

        return ServiceResult<IEnumerable<AvailableShippingMethodDto>>
            .Success(result);
    }
}