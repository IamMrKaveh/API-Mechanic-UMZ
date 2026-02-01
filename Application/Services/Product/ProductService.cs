using Application.Common.Interfaces.Cache;
using Application.Common.Interfaces.Media;
using Application.Common.Interfaces.Product;
using Application.DTOs.Media;
using Application.DTOs.Product;

namespace Application.Services.Product;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediaService _mediaService;
    private readonly ICacheService _cacheService;

    private const string ProductCachePrefix = "product:";
    private const string ProductListCachePrefix = "products:list:";
    private const string AttributesCacheKey = "attributes:all";
    private static readonly TimeSpan ProductCacheExpiry = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan AttributesCacheExpiry = TimeSpan.FromHours(1);

    public ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger,
        IMapper mapper,
        IMediaService mediaService,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _logger = logger;
        _mapper = mapper;
        _mediaService = mediaService;
        _cacheService = cacheService;
    }

    public async Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto searchDto)
    {
        var cacheKey = GenerateProductListCacheKey(searchDto);

        var cached = await _cacheService.GetAsync<PagedResultDto<PublicProductViewDto>>(cacheKey);
        if (cached != null)
        {
            return ServiceResult<PagedResultDto<PublicProductViewDto>>.Ok(cached);
        }

        var (products, totalItems) = await _productRepository.GetPagedAsync(searchDto);

        var productDtos = new List<PublicProductViewDto>();
        foreach (var product in products)
        {
            productDtos.Add(await MapToPublicViewDto(product, searchDto));
        }

        var result = new PagedResultDto<PublicProductViewDto>
        {
            Items = productDtos,
            TotalItems = totalItems,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize
        };

        var tags = productDtos.Select(p => $"product_tag:{p.Id}").ToList();
        await _cacheService.SetAsync(cacheKey, result, ProductCacheExpiry, tags);

        return ServiceResult<PagedResultDto<PublicProductViewDto>>.Ok(result);
    }

    public async Task<ServiceResult<PublicProductViewDto?>> GetProductByIdAsync(int productId, bool includeInactive = false)
    {
        var cacheKey = $"{ProductCachePrefix}{productId}";

        if (!includeInactive)
        {
            var cached = await _cacheService.GetAsync<PublicProductViewDto>(cacheKey);
            if (cached != null)
            {
                return ServiceResult<PublicProductViewDto?>.Ok(cached);
            }
        }

        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, includeInactive);
        if (product == null)
        {
            return ServiceResult<PublicProductViewDto?>.Fail("Product not found.");
        }

        var dto = await MapToPublicViewDto(product, new ProductSearchDto());

        if (!includeInactive)
        {
            await _cacheService.SetAsync(cacheKey, dto, ProductCacheExpiry, [$"product_tag:{productId}"]);
        }

        return ServiceResult<PublicProductViewDto?>.Ok(dto);
    }

    public async Task<ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>> GetAllAttributesAsync()
    {
        var cached = await _cacheService.GetAsync<List<AttributeTypeWithValuesDto>>(AttributesCacheKey);
        if (cached != null)
        {
            return ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>.Ok(cached);
        }

        var attributes = await _productRepository.GetAllAttributeTypesWithValuesAsync();
        var dtos = _mapper.Map<List<AttributeTypeWithValuesDto>>(attributes);

        await _cacheService.SetAsync(AttributesCacheKey, dtos, AttributesCacheExpiry);

        return ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>.Ok(dtos);
    }

    private async Task<PublicProductViewDto> MapToPublicViewDto(Product product, ProductSearchDto searchDto)
    {
        var dto = _mapper.Map<PublicProductViewDto>(product);
        dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        var media = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        dto.Images = _mapper.Map<IEnumerable<MediaDto>>(media);

        var variantsQuery = product.Variants.Where(v => v.IsActive);

        if (searchDto.MinPrice.HasValue)
        {
            variantsQuery = variantsQuery.Where(v => v.SellingPrice >= searchDto.MinPrice.Value);
        }
        if (searchDto.MaxPrice.HasValue)
        {
            variantsQuery = variantsQuery.Where(v => v.SellingPrice <= searchDto.MaxPrice.Value);
        }
        if (searchDto.InStock == true)
        {
            variantsQuery = variantsQuery.Where(v => v.IsUnlimited || v.Stock > 0);
        }
        if (searchDto.HasDiscount == true)
        {
            variantsQuery = variantsQuery.Where(v => v.OriginalPrice > v.SellingPrice);
        }

        var variantDtos = new List<ProductVariantResponseDto>();
        foreach (var variant in variantsQuery)
        {
            var variantDto = _mapper.Map<ProductVariantResponseDto>(variant);
            var variantMedia = await _mediaService.GetEntityMediaAsync("ProductVariant", variant.Id);
            variantDto.Images = _mapper.Map<IEnumerable<MediaDto>>(variantMedia);
            variantDtos.Add(variantDto);
        }
        dto.Variants = variantDtos;

        return dto;
    }

    private string GenerateProductListCacheKey(ProductSearchDto searchDto)
    {
        var keyParts = new List<string>
        {
            ProductListCachePrefix,
            $"p{searchDto.Page}",
            $"ps{searchDto.PageSize}",
            $"s{searchDto.SortBy}"
        };

        if (!string.IsNullOrEmpty(searchDto.Name))
            keyParts.Add($"n{searchDto.Name}");
        if (searchDto.CategoryId.HasValue)
            keyParts.Add($"c{searchDto.CategoryId}");
        if (searchDto.CategoryGroupId.HasValue)
            keyParts.Add($"cg{searchDto.CategoryGroupId}");
        if (searchDto.MinPrice.HasValue)
            keyParts.Add($"min{searchDto.MinPrice}");
        if (searchDto.MaxPrice.HasValue)
            keyParts.Add($"max{searchDto.MaxPrice}");
        if (searchDto.InStock.HasValue)
            keyParts.Add($"is{searchDto.InStock}");
        if (searchDto.HasDiscount.HasValue)
            keyParts.Add($"hd{searchDto.HasDiscount}");

        return string.Join(":", keyParts);
    }
}