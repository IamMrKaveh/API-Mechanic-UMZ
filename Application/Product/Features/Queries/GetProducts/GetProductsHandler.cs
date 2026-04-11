using Application.Product.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Application.Product.Features.Queries.GetProducts;

public class GetProductsHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductsQuery, ServiceResult<PaginatedResult<ProductListItemDto>>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductListItemDto>>> Handle(
        GetProductsQuery request,
        CancellationToken ct)
    {
        var categoryId = CategoryId.From(request.CategoryId.Value);
        var brandId = BrandId.From(request.BrandId.Value);

        var result = await _productQueryService.GetProductsPagedAsync(
            categoryId,
            brandId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductListItemDto>>.Success(result);
    }
}