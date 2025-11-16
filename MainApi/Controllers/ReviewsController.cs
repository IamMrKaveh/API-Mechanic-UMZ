namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(IReviewService reviewService, ICurrentUserService currentUserService, ILogger<ReviewsController> logger)
    {
        _reviewService = reviewService;
        _currentUserService = currentUserService;
        _logger = logger;
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
    [Authorize]
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

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetReviewsByStatus([FromQuery] string status = "Pending", [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _reviewService.GetReviewsByStatusAsync(status, page, pageSize);
        if (!result.Success) return StatusCode(500, new { message = result.Error });
        return Ok(result.Data);
    }

    [HttpPatch("{reviewId}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateReviewStatus(int reviewId, [FromBody] UpdateReviewStatusDto dto)
    {
        var result = await _reviewService.UpdateReviewStatusAsync(reviewId, dto.Status);
        if (result.Success) return NoContent();
        return result.Error == "Review not found" ? NotFound() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{reviewId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var result = await _reviewService.DeleteReviewAsync(reviewId);
        if (result.Success) return NoContent();
        return result.Error == "Review not found" ? NotFound() : BadRequest(new { message = result.Error });
    }
}