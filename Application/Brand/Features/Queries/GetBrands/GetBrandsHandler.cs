using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrands;

public class GetBrandsHandler(
    IBrandQueryService brandQueryService) : IRequestHandler<GetBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(GetBrandsQuery request, CancellationToken ct)
    {
        CategoryId? categoryId = request.CategoryId.HasValue
            ? CategoryId.From(request.CategoryId.Value)
            : null;

        var result = await brandQueryService.GetBrandsPagedAsync(
            categoryId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<BrandListItemDto>>.Success(result);
    }
}