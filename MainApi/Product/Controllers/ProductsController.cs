namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductCatalogSearchParams searchParams)
    {
        var query = new Application.Product.Features.Queries.GetProductCatalog.GetProductCatalogQuery(searchParams);
        var result = await _mediator.Send(query);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResult<PublicProductViewDto?>>> GetById(int id)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }
}