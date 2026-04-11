namespace Application.Product.Features.Queries.GetProductCatalog;

public class GetProductCatalogHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetProductCatalogQuery, ServiceResult<PaginatedResult<ProductCatalogItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductCatalogItemDto>>> Handle(
        GetProductCatalogQuery request, CancellationToken ct)
    {
        var result = await productQueryService.GetProductCatalogAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(result);
    }
}