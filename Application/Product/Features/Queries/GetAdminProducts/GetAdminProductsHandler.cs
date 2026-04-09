using Application.Product.Contracts;
using SharedKernel.Models;

namespace Application.Product.Features.Queries.GetAdminProducts;

public class GetAdminProductsHandler(IProductQueryService productQueryService)
        : IRequestHandler<GetAdminProductsQuery, ServiceResult<PaginatedResult<AdminProductListItemDto>>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PaginatedResult<AdminProductListItemDto>>> Handle(
        GetAdminProductsQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetAdminProductsAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<AdminProductListItemDto>>.Success(result);
    }
}