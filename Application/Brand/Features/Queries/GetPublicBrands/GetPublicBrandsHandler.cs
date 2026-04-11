using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Queries.GetPublicBrands;

public sealed class GetPublicBrandsHandler(IBrandQueryService brandQueryService)
    : IRequestHandler<GetPublicBrandsQuery, ServiceResult<IReadOnlyList<BrandListItemDto>>>
{
    public async Task<ServiceResult<IReadOnlyList<BrandListItemDto>>> Handle(
        GetPublicBrandsQuery request,
        CancellationToken ct)
    {
        CategoryId? categoryId = request.CategoryId.HasValue
            ? CategoryId.From(request.CategoryId.Value)
            : null;

        var brands = await brandQueryService.GetPublicBrandsAsync(categoryId, ct);
        return ServiceResult<IReadOnlyList<BrandListItemDto>>.Success(brands);
    }
}