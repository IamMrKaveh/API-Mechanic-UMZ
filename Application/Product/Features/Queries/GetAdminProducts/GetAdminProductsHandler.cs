namespace Application.Product.Features.Queries.GetAdminProducts;

public class GetAdminProductsHandler
    : IRequestHandler<GetAdminProductsQuery, ServiceResult<PaginatedResult<AdminProductListItemDto>>>
{
    private readonly IProductQueryService _productQueryService;

    public GetAdminProductsHandler(IProductQueryService productQueryService)
    {
        _productQueryService = productQueryService;
    }

    public async Task<ServiceResult<PaginatedResult<AdminProductListItemDto>>> Handle(
        GetAdminProductsQuery request, CancellationToken ct)
    {
        var result = await _productQueryService.GetAdminProductsAsync(request.SearchParams, ct);
        return ServiceResult<PaginatedResult<AdminProductListItemDto>>.Success(result);
    }
}