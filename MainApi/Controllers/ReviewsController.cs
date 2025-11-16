namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUserService)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
    }

    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (productId <= 0) return BadRequest("Invalid product ID");
        var result = await _reviewService.GetProductReviewsAsync(productId, page, pageSize);
        if (!result.Success) return StatusCode(500, new { message = result.Error });
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto createReviewDto)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _reviewService.CreateReviewAsync(createReviewDto, userId.Value);
        if (!result.Success || result.Data == null)
        {
            return StatusCode(500, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetProductReviews), new { productId = result.Data.ProductId }, result.Data);
    }
}