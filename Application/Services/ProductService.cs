namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;
    private readonly IMapper _mapper;
    private readonly IMediaService _mediaService;

    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger, IMapper mapper, IMediaService mediaService)
    {
        _productRepository = productRepository;
        _logger = logger;
        _mapper = mapper;
        _mediaService = mediaService;
    }

    public async Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto searchDto)
    {
        var (products, totalItems) = await _productRepository.GetPagedAsync(searchDto);

        var productDtos = new List<PublicProductViewDto>();
        foreach (var product in products)
        {
            productDtos.Add(await MapToPublicViewDto(product));
        }

        var result = new PagedResultDto<PublicProductViewDto>
        {
            Items = productDtos,
            TotalItems = totalItems,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize
        };
        return ServiceResult<PagedResultDto<PublicProductViewDto>>.Ok(result);
    }

    public async Task<ServiceResult<PublicProductViewDto?>> GetProductByIdAsync(int productId, bool includeInactive = false)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, includeInactive);
        if (product == null)
        {
            return ServiceResult<PublicProductViewDto?>.Fail("Product not found.");
        }
        var dto = await MapToPublicViewDto(product);
        return ServiceResult<PublicProductViewDto?>.Ok(dto);
    }

    public async Task<ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>> GetAllAttributesAsync()
    {
        var attributes = await _productRepository.GetAllAttributeTypesWithValuesAsync();
        var dtos = _mapper.Map<List<AttributeTypeWithValuesDto>>(attributes);
        return ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>.Ok(dtos);
    }

    private async Task<PublicProductViewDto> MapToPublicViewDto(Product product)
    {
        var dto = _mapper.Map<PublicProductViewDto>(product);
        dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        var media = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        dto.Images = _mapper.Map<IEnumerable<MediaDto>>(media);

        var variantDtos = new List<ProductVariantResponseDto>();
        foreach (var variant in product.Variants.Where(v => v.IsActive))
        {
            var variantDto = _mapper.Map<ProductVariantResponseDto>(variant);
            var variantMedia = await _mediaService.GetEntityMediaAsync("ProductVariant", variant.Id);
            variantDto.Images = _mapper.Map<IEnumerable<MediaDto>>(variantMedia);
            variantDtos.Add(variantDto);
        }
        dto.Variants = variantDtos;

        return dto;
    }
}