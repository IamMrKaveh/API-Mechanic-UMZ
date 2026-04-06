using Application.Common.Results;
using Application.Product.Contracts;
using Application.Product.Features.Shared;
using SharedKernel.Models;

namespace Application.Product.Features.Queries.GetProducts;

public class GetProductsHandler(
    IProductQueryService productQueryService) : IRequestHandler<GetProductsQuery, ServiceResult<PaginatedResult<ProductListItemDto>>>
{
    private readonly IProductQueryService _productQueryService = productQueryService;

    public async Task<ServiceResult<PaginatedResult<ProductListItemDto>>> Handle(
        GetProductsQuery request,
        CancellationToken ct)
    {
        var result = await _productQueryService.GetProductsPagedAsync(
            request.CategoryId,
            request.BrandId,
            request.Search,
            request.IsActive,
            request.IncludeDeleted,
            request.Page,
            request.PageSize,
            ct);

        return ServiceResult<PaginatedResult<ProductListItemDto>>.Success(result);
    }
}