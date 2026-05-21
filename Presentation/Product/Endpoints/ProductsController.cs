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
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductCatalogItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductCatalog([FromQuery] GetProductCatalogRequest request)
    {
        var query = Mapper.Map<GetProductCatalogQuery>(request);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
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