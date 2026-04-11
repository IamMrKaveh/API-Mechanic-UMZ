using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Queries.GetProducts;
using MapsterMapper;
using Presentation.Product.Mapping;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] GetProductsRequest request)
    {
        var query = Mapper
            .Map<GetProductsQuery>(request);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(
        Guid id,
        [FromBody] GetProductRequest request)
    {
        var query = Mapper
            .Map<GetProductQuery>(request)
            .Enrich(id);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("/catalog")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductCatalog(
        [FromBody] GetProductCatalogRequest request)
    {
        var query = Mapper
            .Map<GetProductCatalogQuery>(request);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpGet("/details")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductDetails(
        Guid id,
        [FromBody] GetProductDetailsRequest request)
    {
        var query = Mapper
            .Map<GetProductDetailsQuery>(request)
            .Enrich(id);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }
}