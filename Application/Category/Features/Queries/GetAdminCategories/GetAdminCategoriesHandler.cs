using Application.Category.Contracts;
using Application.Category.Features.Shared;
using SharedKernel.Models;

namespace Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesHandler(
    ICategoryQueryService categoryQueryService,
    IMapper mapper) : IRequestHandler<GetAdminCategoriesQuery, ServiceResult<PaginatedResult<CategoryListItemDto>>>
{
    private readonly ICategoryQueryService _categoryQueryService = categoryQueryService;
    private readonly IMapper _mapper = mapper;

    public async Task<ServiceResult<PaginatedResult<CategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request,
        CancellationToken ct)
    {
        var (categories, totalCount) = await _categoryQueryService.GetPagedAsync(
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        var dtos = _mapper.Map<List<CategoryListItemDto>>(categories);

        var result = PaginatedResult<CategoryListItemDto>.Create(dtos, totalCount, request.Page, request.PageSize);
        return ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(result);
    }
}