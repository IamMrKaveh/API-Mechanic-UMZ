namespace Application.Variant.Features.Queries.GetProductVariants;

public class GetProductVariantsHandler(IVariantQueryService variantQueryService)
        : IRequestHandler<GetProductVariantsQuery, ServiceResult<IEnumerable<ProductVariantViewDto>>>
{
    private readonly IVariantQueryService _variantQueryService = variantQueryService;

    public async Task<ServiceResult<IEnumerable<ProductVariantViewDto>>> Handle(
        GetProductVariantsQuery request,
        CancellationToken ct)
    {
        var result = await _variantQueryService.GetProductVariantsAsync(
            request.ProductId, request.ActiveOnly, ct);

        return ServiceResult<IEnumerable<ProductVariantViewDto>>.Success(result);
    }
}