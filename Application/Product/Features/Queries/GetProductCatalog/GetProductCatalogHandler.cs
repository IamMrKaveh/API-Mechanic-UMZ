namespace Application.Product.Features.Queries.GetProductCatalog;

public class GetProductCatalogHandler
    : IRequestHandler<GetProductCatalogQuery, ServiceResult<PaginatedResult<ProductCatalogItemDto>>>
{
    private readonly IProductQueryService _productQueryService;

    public GetProductCatalogHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<ProductCatalogItemDto>>> Handle(
        GetProductCatalogQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetProductCatalogAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(result);
    }
}