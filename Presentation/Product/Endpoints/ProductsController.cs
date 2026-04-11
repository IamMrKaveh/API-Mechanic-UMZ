using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Queries.GetProducts;
using MapsterMapper;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] GetProductsRequest request)
    {
        var query = Mapper.Map<GetProductsQuery>(request);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetProductQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("catalog")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductCatalog([FromQuery] GetProductCatalogRequest request)
    {
        var query = Mapper.Map<GetProductCatalogQuery>(request);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("details/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductDetails(Guid id)
    {
        var query = new GetProductDetailsQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }
}