using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetProducts;

public sealed class GetProductsHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductsQuery, ServiceResult<PaginatedResult<ProductListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductListItemDto>>> Handle(
        GetProductsQuery request,
        CancellationToken ct)
    {
        var result = await productQueryService.GetAdminProductsAsync(
            categoryId: request.CategoryId,
            brandId: request.BrandId,
            search: request.Search,
            isActive: request.IsActive,
            includeDeleted: request.IncludeDeleted,
            page: request.Page,
            pageSize: request.PageSize,
            ct: ct);

        return ServiceResult<PaginatedResult<ProductListItemDto>>.Success(result);
    }
}