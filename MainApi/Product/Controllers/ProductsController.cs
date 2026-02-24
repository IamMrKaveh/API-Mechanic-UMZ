namespace MainApi.Product.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : BaseApiController
{
    private readonly IMediator _mediator;
    private readonly ISearchService _searchService;

    public ProductsController(IMediator mediator, ICurrentUserService currentUserService, ISearchService searchService)
        : base(currentUserService)
    {
        _mediator = mediator;
        _searchService = searchService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductCatalogSearchParams searchParams, CancellationToken ct)
    {
        
        var searchParamsObj = new SearchProductsParams
        {
            Q = searchParams.Search ?? string.Empty,
            CategoryId = searchParams.CategoryId,
            BrandId = searchParams.BrandId,
            MinPrice = searchParams.MinPrice,
            MaxPrice = searchParams.MaxPrice,
            InStockOnly = searchParams.InStockOnly,
            SortBy = searchParams.SortBy,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        };

        var result = await _searchService.SearchProductsAsync(searchParamsObj, ct);

        
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var query = new GetProductByIdQuery(id);
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