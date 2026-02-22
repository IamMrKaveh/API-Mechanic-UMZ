namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public class GetAvailableShippingsForVariantsHandler
    : IRequestHandler<
        GetAvailableShippingsForVariantsQuery,
        ServiceResult<IEnumerable<AvailableShippingDto>>>
{
    private readonly IShippingQueryService _shippingQueryService;

    public GetAvailableShippingsForVariantsHandler(
        IShippingQueryService shippingQueryService)
    {
        _shippingQueryService = shippingQueryService;
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingDto>>> Handle(
        GetAvailableShippingsForVariantsQuery request,
        CancellationToken ct)
    {
        if (request.VariantIds == null || !request.VariantIds.Any())
            return ServiceResult<IEnumerable<AvailableShippingDto>>
                .Success(Array.Empty<AvailableShippingDto>());

        var result = await _shippingQueryService
            .GetAvailableShippingsForVariantsAsync(
                request.VariantIds,
                ct);

        return ServiceResult<IEnumerable<AvailableShippingDto>>
            .Success(result);
    }
}