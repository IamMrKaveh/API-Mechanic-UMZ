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

        var dtos = new List<CategoryGroupViewDto>();
        foreach (var group in groups)
        {
            var dto = _mapper.Map<CategoryGroupViewDto>(group);
            dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id);
            dto.ProductCount = group.Products.Count(p => !p.IsDeleted && p.IsActive);
            dtos.Add(dto);
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
        dto.ProductCount = group.Products.Count(p => !p.IsDeleted && p.IsActive);

        return ServiceResult<CategoryGroupViewDto?>.Ok(dto);
    }
}