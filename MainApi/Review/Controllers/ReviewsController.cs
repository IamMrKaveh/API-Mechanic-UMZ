namespace MainApi.Review.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController(IMediator mediator, ICurrentUserService currentUserService) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ICurrentUserService _currentUserService = currentUserService;

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
        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, result);

        return CreatedAtAction(
            nameof(GetProductReviews),
            new { productId = request.ProductId },
            result);
    }

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