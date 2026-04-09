using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Review.Features.Queries.GetProductReviewSummary;
using Presentation.Product.Requests;

namespace Presentation.Product.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class ProductsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts([FromQuery] ProductCatalogSearchRequest request)
    {
        var query = new GetProductCatalogQuery(
            request.Page,
            request.PageSize,
            request.Search,
            request.CategoryId,
            request.BrandId,
            request.MinPrice,
            request.MaxPrice,
            request.InStockOnly,
            request.SortBy);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var query = new GetProductQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}/reviews/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviewSummary(Guid id)
    {
        var query = new GetProductReviewSummaryQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}