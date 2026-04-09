using Application.Review.Features.Commands.SubmitReview;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetUserReviews;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new GetProductReviewsQuery(productId, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewRequest request)
    {
        var command = new SubmitReviewCommand(
            request.ProductId,
            CurrentUser.UserId,
            request.OrderId,
            request.Rating,
            request.Title,
            request.Comment);

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
            return ToActionResult(result);

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
        var result = await _mediator.Send(new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize));
        return ToActionResult(result);
    }
}