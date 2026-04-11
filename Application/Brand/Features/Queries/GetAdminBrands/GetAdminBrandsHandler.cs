using Application.Brand.Features.Shared;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Queries.GetAdminBrands;

public class GetAdminBrandsHandler(IBrandQueryService brandQueryService) : IRequestHandler<GetAdminBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(
        GetAdminBrandsQuery request,
        CancellationToken ct)
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