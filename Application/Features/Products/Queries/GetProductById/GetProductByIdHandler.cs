namespace Application.Features.Products.Queries.GetProductById;

public class GetProductByIdHandler : IRequestHandler<GetProductByIdQuery, ServiceResult<PublicProductViewDto?>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMediaService _mediaService;
    private readonly IMapper _mapper;

    public GetProductByIdHandler(
        IProductRepository productRepository,
        IMediaService mediaService,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mediaService = mediaService;
        _mapper = mapper;
    }

    public async Task<ServiceResult<PublicProductViewDto?>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(request.Id);
        if (product == null || (!product.IsActive && !product.IsDeleted)) // منطق نمایش
        {
            return ServiceResult<PublicProductViewDto?>.Fail("Product not found", 404);
        }

        var dto = _mapper.Map<PublicProductViewDto>(product);

        // Load Media
        var mediaDtos = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        dto.Images = mediaDtos;
        dto.IconUrl = mediaDtos.FirstOrDefault(x => x.IsPrimary)?.Url;

        return ServiceResult<PublicProductViewDto?>.Ok(dto);
    }
}