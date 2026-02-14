using Application.Common.Features.Shared;
using Application.Product.Features.Queries.GetProductById;
using Application.Product.Features.Shared;

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

    [HttpGet("{id}")]
    public async Task<ActionResult<ServiceResult<PublicProductViewDto?>>> GetById(int id)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);

        return Ok(result);
    }

    // Add search endpoint here using GetProductsQuery (Public)
}