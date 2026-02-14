namespace MainApi.Product.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ReviewsController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get approved reviews for a product (public)
    /// </summary>
    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(
        int productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetProductReviewsQuery(productId, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Submit a new review (authenticated user)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var command = new SubmitReviewCommand
        {
            ProductId = request.ProductId,
            UserId = userId.Value,
            OrderId = request.OrderId,
            Rating = request.Rating,
            Title = request.Title,
            Comment = request.Comment
        };

        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);

        return CreatedAtAction(
            nameof(GetProductReviews),
            new { productId = request.ProductId },
            result);
    }

    /// <summary>
    /// Get current user's reviews
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var query = new GetUserReviewsQuery(userId.Value, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}

/// <summary>
/// Request DTO for submit review endpoint
/// </summary>
public class SubmitReviewRequest
{
    public int ProductId { get; set; }
    public int? OrderId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
}