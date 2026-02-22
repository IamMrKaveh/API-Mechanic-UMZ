namespace Application.Category.Features.Queries.GetAdminCategories;

public class GetAdminCategoriesHandler
    : IRequestHandler<GetAdminCategoriesQuery, ServiceResult<PaginatedResult<CategoryListItemDto>>>
{
    private readonly ICategoryRepository _repository;
    private readonly IMapper _mapper;

    public GetAdminCategoriesHandler(ICategoryRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PaginatedResult<CategoryListItemDto>>> Handle(
        GetAdminCategoriesQuery request, CancellationToken cancellationToken)
    {
        var (categories, totalCount) = await _repository.GetPagedAsync(
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = _mapper.Map<List<CategoryListItemDto>>(categories);

        var result = PaginatedResult<CategoryListItemDto>.Create(dtos, totalCount, request.Page, request.PageSize);
        return ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(result);
    }
}