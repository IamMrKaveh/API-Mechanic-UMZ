using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Application.Brand.Features.Queries.GetBrands;

public class GetBrandsHandler(
    IBrandQueryService brandQueryService) : IRequestHandler<GetBrandsQuery, ServiceResult<PaginatedResult<BrandListItemDto>>>
{
    private readonly IBrandQueryService _brandQueryService = brandQueryService;

    public async Task<ServiceResult<PaginatedResult<BrandListItemDto>>> Handle(GetBrandsQuery request, CancellationToken ct)
    {
        CategoryId? categoryId = request.CategoryId.HasValue
            ? CategoryId.From(request.CategoryId.Value)
            : null;

        var result = await _brandQueryService.GetBrandsPagedAsync(
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