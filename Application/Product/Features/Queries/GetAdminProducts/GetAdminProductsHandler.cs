using Application.Product.Features.Shared;

namespace Application.Product.Features.Queries.GetAdminProducts;

public sealed class GetAdminProductsHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetAdminProductsQuery, ServiceResult<PaginatedResult<ProductListItemDto>>>
{
    public async Task<ServiceResult<PaginatedResult<ProductListItemDto>>> Handle(
        GetAdminProductsQuery request,
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