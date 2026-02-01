namespace Application.Features.Products.Queries.GetAdminProducts;

public record GetAdminProductsQuery(ProductSearchDto SearchDto) : IRequest<ServiceResult<PagedResultDto<AdminProductListDto>>>;

public class GetAdminProductsQueryHandler : IRequestHandler<GetAdminProductsQuery, ServiceResult<PagedResultDto<AdminProductListDto>>>
{
    private readonly IAdminProductService _legacyService;

    public GetAdminProductsQueryHandler(IAdminProductService legacyService)
    {
        _legacyService = legacyService;
    }

    public async Task<ServiceResult<PagedResultDto<AdminProductListDto>>> Handle(GetAdminProductsQuery request, CancellationToken cancellationToken)
    {
        // Bridge to existing logic until Repository is fully exposed or logic moved here
        return await _legacyService.GetProductsAsync(
            request.SearchDto.Name,
            request.SearchDto.CategoryId,
            request.SearchDto.IncludeInactive == true ? null : true, // Logic mapping
            request.SearchDto.IncludeDeleted ?? false,
            request.SearchDto.Page,
            request.SearchDto.PageSize);
    }
}