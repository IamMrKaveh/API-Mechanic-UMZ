namespace Application.Services;

public class AdminCategoryGroupService : IAdminCategoryGroupService
{
    private readonly ICategoryGroupRepository _repository;
    private readonly IMediaService _mediaService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminCategoryGroupService> _logger;

    public AdminCategoryGroupService(
        ICategoryGroupRepository repository,
        IMediaService mediaService,
        IHtmlSanitizer htmlSanitizer,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<AdminCategoryGroupService> logger)
    {
        _repository = repository;
        _mediaService = mediaService;
        _htmlSanitizer = htmlSanitizer;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

    public async Task<ServiceResult<CategoryGroupViewDto>> CreateAsync(CategoryGroupCreateDto dto)
    {
        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
        if (await _repository.ExistsAsync(sanitizedName, dto.CategoryId))
        {
            return ServiceResult<CategoryGroupViewDto>.Fail("A group with this name already exists in this category.");
        }

        var group = _mapper.Map<Domain.Category.CategoryGroup>(dto);
        group.Name = sanitizedName;

        await _repository.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        if (dto.IconFile != null)
        {
            try
            {
                await _mediaService.AttachFileToEntityAsync(dto.IconFile.OpenReadStream(), dto.IconFile.FileName, dto.IconFile.ContentType, dto.IconFile.Length, "CategoryGroup", group.Id, true);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload icon for new category group {CategoryGroupId}", group.Id);
            }
        }

        var resultDto = _mapper.Map<CategoryGroupViewDto>(group);
        resultDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id);

        return ServiceResult<CategoryGroupViewDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateAsync(int id, CategoryGroupUpdateDto dto)
    {
        var group = await _repository.GetByIdAsync(id);
        if (group == null)
        {
            return ServiceResult.Fail("Category group not found.");
        }

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            _repository.SetOriginalRowVersion(group, Convert.FromBase64String(dto.RowVersion));
        }

        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
        if (await _repository.ExistsAsync(sanitizedName, dto.CategoryId, id))
        {
            return ServiceResult.Fail("A group with this name already exists in this category.");
        }

        _mapper.Map(dto, group);
        group.Name = sanitizedName;
        _repository.Update(group);

        if (dto.IconFile != null)
        {
            var primaryMedia = (await _mediaService.GetEntityMediaAsync("CategoryGroup", id)).FirstOrDefault(m => m.IsPrimary);
            if (primaryMedia != null)
            {
                await _mediaService.DeleteMediaAsync(primaryMedia.Id);
            }
            await _mediaService.AttachFileToEntityAsync(dto.IconFile.OpenReadStream(), dto.IconFile.FileName, dto.IconFile.ContentType, dto.IconFile.Length, "CategoryGroup", id, true);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This record was modified by another user. Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var group = await _repository.GetByIdWithProductsAsync(id);
        if (group == null)
        {
            return ServiceResult.Fail("Category group not found.");
        }

        if (group.Products.Any())
        {
            return ServiceResult.Fail("Cannot delete group with associated products.");
        }

        var media = await _mediaService.GetEntityMediaAsync("CategoryGroup", id);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        _repository.Delete(group);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}