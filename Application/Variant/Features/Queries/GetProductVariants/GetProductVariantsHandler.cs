namespace Application.Variant.Features.Queries.GetProductVariants;

public class GetProductVariantsHandler
    : IRequestHandler<GetProductVariantsQuery, ServiceResult<IEnumerable<ProductVariantViewDto>>>
{
    private readonly IProductQueryService _productQueryService;

    public GetProductVariantsHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<IEnumerable<ProductVariantViewDto>>> Handle(
        GetProductVariantsQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetProductVariantsAsync(
            request.ProductId, request.ActiveOnly, ct);

        return ServiceResult<IEnumerable<ProductVariantViewDto>>.Success(result);
    }
}