namespace Application.Services;

public class CategoryGroupService : ICategoryGroupService
{
    private readonly ICategoryGroupRepository _repository;
    private readonly IMediaService _mediaService;
    private readonly IMapper _mapper;

    public CategoryGroupService(
        ICategoryGroupRepository repository,
        IMediaService mediaService,
        IMapper mapper)
    {
        _repository = repository;
        _mediaService = mediaService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PagedResultDto<CategoryGroupViewDto>>> GetPagedAsync(int? categoryId, string? search, int page, int pageSize)
    {
        var (groups, total) = await _repository.GetPagedAsync(categoryId, search, page, pageSize);

        var dtos = _mapper.Map<List<CategoryGroupViewDto>>(groups);

        foreach (var dto in dtos)
        {
            dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", dto.Id);
        }

        var result = new PagedResultDto<CategoryGroupViewDto>
        {
            Items = dtos,
            TotalItems = total,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResultDto<CategoryGroupViewDto>>.Ok(result);
    }

    public async Task<ServiceResult<CategoryGroupViewDto?>> GetByIdAsync(int id)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
        {
            return ServiceResult<CategoryGroupViewDto?>.Fail("Category group not found.");
        }

        var dto = _mapper.Map<CategoryGroupViewDto>(group);
        dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", id);

        return ServiceResult<CategoryGroupViewDto?>.Ok(dto);
    }
}