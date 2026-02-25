namespace MainApi.Review.Controllers;

[Route("api/admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReviewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetReviewsByStatusQuery(status, page, pageSize);
        var result = await _mediator.Send(query);
        return result.IsSucceed ? Ok(result.Data) : StatusCode(result.StatusCode, result);
    }

    [HttpPatch("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(int reviewId)
    {
        var command = new ApproveReviewCommand(reviewId);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPatch("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(int reviewId, [FromBody] RejectReviewRequest request)
    {
        var command = new RejectReviewCommand(reviewId, request.Reason);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPatch("{reviewId}/status")]
    public async Task<IActionResult> UpdateReviewStatus(int reviewId, [FromBody] UpdateReviewStatusRequest request)
    {
        var command = new UpdateReviewStatusCommand(reviewId, request.Status);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpPost("{reviewId}/reply")]
    public async Task<IActionResult> ReplyToReview(int reviewId, [FromBody] ReplyToReviewRequest request)
    {
        var command = new ReplyToReviewCommand(reviewId, request.Reply);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(int reviewId)
    {
        var command = new DeleteReviewCommand(reviewId);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }
}