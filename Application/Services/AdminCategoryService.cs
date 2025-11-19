namespace Application.Services;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMediaService _mediaService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdminCategoryService> _logger;

    public AdminCategoryService(
        ICategoryRepository categoryRepository,
        IMediaService mediaService,
        IHtmlSanitizer htmlSanitizer,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<AdminCategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _mediaService = mediaService;
        _htmlSanitizer = htmlSanitizer;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<PagedResultDto<CategoryViewDto>>> GetCategoriesAsync(string? search, int page, int pageSize)
    {
        var (categories, totalItems) = await _categoryRepository.GetCategoriesAsync(search, page, pageSize);
        var categoryDtos = new List<CategoryViewDto>();

        foreach (var category in categories)
        {
            var dto = _mapper.Map<CategoryViewDto>(category);
            dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

            var groupDtos = new List<CategoryGroupSummaryDto>();
            foreach (var group in category.CategoryGroups)
            {
                var groupDto = _mapper.Map<CategoryGroupSummaryDto>(group);
                groupDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id);
                groupDtos.Add(groupDto);
            }
            dto.CategoryGroups = groupDtos;
            categoryDtos.Add(dto);
        }

        var pagedResult = new PagedResultDto<CategoryViewDto>
        {
            Items = categoryDtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResultDto<CategoryViewDto>>.Ok(pagedResult);
    }

    public async Task<ServiceResult<CategoryDetailViewDto?>> GetCategoryByIdAsync(int id, int page, int pageSize)
    {
        var category = await _categoryRepository.GetCategoryWithGroupsByIdAsync(id);
        if (category == null)
        {
            return ServiceResult<CategoryDetailViewDto?>.Fail("Category not found.");
        }

        var (products, totalProductCount) = await _categoryRepository.GetProductsByCategoryIdAsync(id, page, pageSize);

        var categoryDto = _mapper.Map<CategoryDetailViewDto>(category);
        categoryDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

        var productDtos = new List<ProductSummaryDto>();
        foreach (var product in products)
        {
            var productDto = _mapper.Map<ProductSummaryDto>(product);
            productDto.Icon = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
            productDtos.Add(productDto);
        }

        categoryDto.Products = new PagedResultDto<ProductSummaryDto>
        {
            Items = productDtos,
            TotalItems = totalProductCount,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<CategoryDetailViewDto?>.Ok(categoryDto);
    }

    public async Task<ServiceResult<CategoryViewDto>> CreateCategoryAsync(CategoryCreateDto dto)
    {
        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
        if (await _categoryRepository.ExistsByNameAsync(sanitizedName))
        {
            return ServiceResult<CategoryViewDto>.Fail("A category with this name already exists.");
        }

        var category = _mapper.Map<Category>(dto);
        category.Name = sanitizedName;
        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        if (dto.IconFile != null)
        {
            try
            {
                await _mediaService.AttachFileToEntityAsync(dto.IconFile.OpenReadStream(), dto.IconFile.FileName, dto.IconFile.ContentType, dto.IconFile.Length, "Category", category.Id, true);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload icon for new category {CategoryId}", category.Id);
            }
        }


        var resultDto = _mapper.Map<CategoryViewDto>(category);
        resultDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

        return ServiceResult<CategoryViewDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateCategoryAsync(int id, CategoryUpdateDto dto)
    {
        var existingCategory = await _categoryRepository.GetCategoryWithGroupsByIdAsync(id);
        if (existingCategory == null)
        {
            return ServiceResult.Fail("Category not found.");
        }

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            _categoryRepository.SetOriginalRowVersion(existingCategory, Convert.FromBase64String(dto.RowVersion));
        }

        var sanitizedName = _htmlSanitizer.Sanitize(dto.Name.Trim());
        if (await _categoryRepository.ExistsByNameAsync(sanitizedName, id))
        {
            return ServiceResult.Fail("A category with this name already exists.");
        }

        _mapper.Map(dto, existingCategory);
        existingCategory.Name = sanitizedName;
        _categoryRepository.Update(existingCategory);

        if (dto.IconFile != null)
        {
            var primaryMedia = (await _mediaService.GetEntityMediaAsync("Category", id)).FirstOrDefault(m => m.IsPrimary);
            if (primaryMedia != null)
            {
                await _mediaService.DeleteMediaAsync(primaryMedia.Id);
            }
            await _mediaService.AttachFileToEntityAsync(dto.IconFile.OpenReadStream(), dto.IconFile.FileName, dto.IconFile.ContentType, dto.IconFile.Length, "Category", id, true);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("The record you attempted to edit was modified by another user. Please reload and try again.");
        }
    }

    public async Task<ServiceResult> DeleteCategoryAsync(int id)
    {
        var category = await _categoryRepository.GetCategoryWithProductsAsync(id);
        if (category == null)
        {
            return ServiceResult.Fail("Category not found.");
        }

        if (category.CategoryGroups.Any(cg => cg.Products.Any()))
        {
            return ServiceResult.Fail("Cannot delete a category that has associated products in its groups.");
        }

        var media = await _mediaService.GetEntityMediaAsync("Category", id);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        foreach (var group in category.CategoryGroups)
        {
            var groupMedia = await _mediaService.GetEntityMediaAsync("CategoryGroup", group.Id);
            foreach (var m in groupMedia)
            {
                await _mediaService.DeleteMediaAsync(m.Id);
            }
        }

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        _categoryRepository.Update(category);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}