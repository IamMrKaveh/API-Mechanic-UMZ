using Application.Common.Interfaces.Cache;
using Application.Common.Interfaces.Category;
using Application.Common.Interfaces.Media;
using Application.DTOs.Category;
using Application.DTOs.Product;

namespace Application.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICategoryGroupRepository _categoryGroupRepository;
    private readonly IMediaService _mediaService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CategoryService> _logger;

    private const string CategoryHierarchyCacheKey = "categories:hierarchy";
    private const string CategoriesCachePrefix = "categories:list:";
    private const string CategoryDetailCachePrefix = "categories:detail:";
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

    public CategoryService(
        ICategoryRepository categoryRepository,
        ICategoryGroupRepository categoryGroupRepository,
        IMediaService mediaService,
        IHtmlSanitizer htmlSanitizer,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<CategoryService> logger)
    {
        _categoryRepository = categoryRepository;
        _categoryGroupRepository = categoryGroupRepository;
        _mediaService = mediaService;
        _htmlSanitizer = htmlSanitizer;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ServiceResult<IEnumerable<CategoryHierarchyDto>>> GetCategoryHierarchyAsync()
    {
        var cached = await _cacheService.GetAsync<List<CategoryHierarchyDto>>(CategoryHierarchyCacheKey);
        if (cached != null)
        {
            return ServiceResult<IEnumerable<CategoryHierarchyDto>>.Ok(cached);
        }

        var categories = await _categoryRepository.GetAllCategoriesWithGroupsAsync();

        var hierarchy = categories.Select(c => new CategoryHierarchyDto
        {
            Id = c.Id,
            Title = c.Name,
            Groups = c.CategoryGroups.Select(cg => new CategoryGroupHierarchyDto
            {
                Id = cg.Id,
                Title = cg.Name
            }).ToList()
        }).ToList();

        await _cacheService.SetAsync(CategoryHierarchyCacheKey, hierarchy, CacheExpiry);

        return ServiceResult<IEnumerable<CategoryHierarchyDto>>.Ok(hierarchy);
    }

    public async Task<ServiceResult<PagedResultDto<CategoryViewDto>>> GetCategoriesAsync(string? search, int page, int pageSize)
    {
        var cacheKey = $"{CategoriesCachePrefix}{search ?? "all"}:{page}:{pageSize}";

        if (string.IsNullOrEmpty(search))
        {
            var cached = await _cacheService.GetAsync<PagedResultDto<CategoryViewDto>>(cacheKey);
            if (cached != null)
            {
                return ServiceResult<PagedResultDto<CategoryViewDto>>.Ok(cached);
            }
        }

        var (categories, totalItems) = await _categoryRepository.GetCategoriesAsync(search, page, pageSize);
        var categoryDtos = new List<CategoryViewDto>();

        foreach (var category in categories)
        {
            var dto = _mapper.Map<CategoryViewDto>(category);
            dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

            var groupDtos = new List<CategoryGroupSummaryDto>();
            foreach (var group in category.CategoryGroups.Where(g => !g.IsDeleted))
            {
                var activeProducts = group.Products.Where(p => !p.IsDeleted && p.IsActive).ToList();
                var groupDto = new CategoryGroupSummaryDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id),
                    ProductCount = activeProducts.Count,
                    InStockProducts = activeProducts.Count(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)),
                    TotalValue = activeProducts.Sum(p => p.Variants.Where(v => !v.IsDeleted).Sum(v => v.PurchasePrice * v.Stock)),
                    TotalSellingValue = activeProducts.Sum(p => p.Variants.Where(v => !v.IsDeleted).Sum(v => v.SellingPrice * v.Stock))
                };
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

        if (string.IsNullOrEmpty(search))
        {
            await _cacheService.SetAsync(cacheKey, pagedResult, CacheExpiry);
        }

        return ServiceResult<PagedResultDto<CategoryViewDto>>.Ok(pagedResult);
    }

    public async Task<ServiceResult<CategoryDetailViewDto?>> GetCategoryByIdAsync(int id, int page, int pageSize)
    {
        var cacheKey = $"{CategoryDetailCachePrefix}{id}:{page}:{pageSize}";

        var cached = await _cacheService.GetAsync<CategoryDetailViewDto>(cacheKey);
        if (cached != null)
        {
            return ServiceResult<CategoryDetailViewDto?>.Ok(cached);
        }

        var category = await _categoryRepository.GetCategoryWithGroupsByIdAsync(id);
        if (category == null)
        {
            return ServiceResult<CategoryDetailViewDto?>.Fail("Category not found.");
        }

        var (products, totalProductCount) = await _categoryRepository.GetProductsByCategoryIdAsync(id, page, pageSize);

        var categoryDto = _mapper.Map<CategoryDetailViewDto>(category);
        categoryDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

        var groupDtos = new List<CategoryGroupSummaryDto>();
        foreach (var group in category.CategoryGroups.Where(g => !g.IsDeleted))
        {
            var activeProducts = group.Products.Where(p => !p.IsDeleted && p.IsActive).ToList();
            var groupDto = new CategoryGroupSummaryDto
            {
                Id = group.Id,
                Name = group.Name,
                IconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id),
                ProductCount = activeProducts.Count,
                InStockProducts = activeProducts.Count(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)),
                TotalValue = activeProducts.Sum(p => p.Variants.Where(v => !v.IsDeleted).Sum(v => v.PurchasePrice * v.Stock)),
                TotalSellingValue = activeProducts.Sum(p => p.Variants.Where(v => !v.IsDeleted).Sum(v => v.SellingPrice * v.Stock))
            };
            groupDtos.Add(groupDto);
        }
        categoryDto.CategoryGroups = groupDtos;

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

        await _cacheService.SetAsync(cacheKey, categoryDto, CacheExpiry);

        return ServiceResult<CategoryDetailViewDto?>.Ok(categoryDto);
    }
}