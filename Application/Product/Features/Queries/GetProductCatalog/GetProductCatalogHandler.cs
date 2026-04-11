namespace Application.Product.Features.Queries.GetProductCatalog;

public class GetProductCatalogHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetProductCatalogQuery, ServiceResult<PaginatedResult<ProductCatalogItemsDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductCatalogItemsDto>>> Handle(
        GetProductCatalogQuery request, CancellationToken ct)
    {
        var result = await productQueryService.GetProductCatalogAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<ProductCatalogItemsDto>>.Success(result);
    }
}