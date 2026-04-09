using Application.Product.Contracts;
using SharedKernel.Models;

namespace Application.Product.Features.Queries.GetProductCatalog;

public class GetProductCatalogHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetProductCatalogQuery, ServiceResult<PaginatedResult<ProductCatalogItemDto>>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductCatalogItemDto>>> Handle(
        GetProductCatalogQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetProductCatalogAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(result);
    }
}