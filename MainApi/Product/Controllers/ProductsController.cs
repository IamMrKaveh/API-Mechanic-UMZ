using Application.Product.Features.Queries.GetProductById;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Shared;
using Application.Review.Features.Queries.GetProductReviewSummary;

namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductCatalogSearchParams searchParams,
        CancellationToken ct)
    {
        var query = new GetProductCatalogQuery(searchParams);
        var result = await _mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}/details")]
    public async Task<IActionResult> GetDetails(int id)
    {
        var query = new GetProductDetailsQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}/reviews/summary")]
    public async Task<IActionResult> GetReviewSummary(int id)
    {
        var query = new GetProductReviewSummaryQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}