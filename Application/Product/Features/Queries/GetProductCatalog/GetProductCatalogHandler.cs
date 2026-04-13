using Application.Product.Contracts;
using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProductCatalog;

public sealed class GetProductCatalogHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductCatalogQuery, ServiceResult<PaginatedResult<ProductCatalogItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductCatalogItemDto>>> Handle(
        GetProductCatalogQuery request, CancellationToken ct)
    {
        var searchParams = new ProductCatalogSearchParams(
            request.Page,
            request.PageSize,
            request.Search,
            request.CategoryId,
            request.BrandId,
            request.MinPrice,
            request.MaxPrice,
            request.InStockOnly,
            request.SortBy);

        var result = await productQueryService.GetProductCatalogAsync(searchParams, ct);
        return ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(result);
    }
}