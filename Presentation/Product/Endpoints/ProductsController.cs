using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Queries.GetProducts;
using Application.Product.Features.Shared;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/v{version:apiVersion}/products")]
[ApiController]
[AllowAnonymous]
public class ProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsRequest request)
    {
        var query = Mapper.Map<GetProductsQuery>(request);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetProductQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("catalog")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductCatalogItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCatalog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] bool? hasDiscount = null,
        CancellationToken ct = default)
    {
        return await Send(new GetProductCatalogQuery(
            page, pageSize, search, categoryId, brandId,
            minPrice, maxPrice, inStockOnly, sortBy, isFeatured, hasDiscount), ct);
    }

    [HttpGet("discounted")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductCatalogItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDiscountedProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? sortBy = null,
        CancellationToken ct = default)
    {
        return await Send(new GetProductCatalogQuery(
            Page: page,
            PageSize: pageSize,
            SortBy: sortBy,
            HasDiscount: true), ct);
    }

    [HttpGet("{id:guid}/details")]
    [ProducesResponseType(typeof(ApiResponse<PublicProductDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductDetails(Guid id)
    {
        var query = new GetProductDetailsQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }
}
