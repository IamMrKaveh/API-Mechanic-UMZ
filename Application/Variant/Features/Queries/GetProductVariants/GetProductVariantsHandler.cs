using Domain.Product.ValueObjects;

namespace Application.Variant.Features.Queries.GetProductVariants;

public class GetProductVariantsHandler(IVariantQueryService variantQueryService)
        : IRequestHandler<GetProductVariantsQuery, ServiceResult<IEnumerable<ProductVariantViewDto>>>
{
    public async Task<ServiceResult<IEnumerable<ProductVariantViewDto>>> Handle(
        GetProductVariantsQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await variantQueryService.GetProductVariantsAsync(
            request.ProductId, request.ActiveOnly, ct);

        return ServiceResult<IEnumerable<ProductVariantViewDto>>.Success(result);
    }
}