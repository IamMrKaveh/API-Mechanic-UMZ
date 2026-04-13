using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;

namespace Application.Variant.Features.Queries.GetVariants;

public class GetVariantsHandler(IVariantQueryService variantQueryService)
    : IRequestHandler<GetVariantsQuery, ServiceResult<IEnumerable<ProductVariantViewDto>>>
{
    public async Task<ServiceResult<IEnumerable<ProductVariantViewDto>>> Handle(
        GetVariantsQuery request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);

        var result = await variantQueryService.GetProductVariantsAsync(productId, request.ActiveOnly, ct);

        return ServiceResult<IEnumerable<ProductVariantViewDto>>.Success(result);
    }
}