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

    /// <summary>
    /// Get reviews by status (admin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetPendingReviewsQuery(status, page, pageSize);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    /// <summary>
    /// Approve a review
    /// </summary>
    [HttpPatch("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(int reviewId)
    {
        var command = new ApproveReviewCommand(reviewId);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    /// <summary>
    /// Reject a review
    /// </summary>
    [HttpPatch("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(int reviewId, [FromBody] RejectReviewRequest request)
    {
        var command = new RejectReviewCommand(reviewId, request.Reason);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    /// <summary>
    /// Reply to a review (admin)
    /// </summary>
    [HttpPost("{reviewId}/reply")]
    public async Task<IActionResult> ReplyToReview(int reviewId, [FromBody] ReplyToReviewRequest request)
    {
        var command = new ReplyToReviewCommand(reviewId, request.Reply);
        var result = await _mediator.Send(command);
        if (!result.IsSucceed)
            return StatusCode(result.StatusCode, result);
        return NoContent();
    }

    /// <summary>
    /// Delete a review (soft-delete)
    /// </summary>
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

/// <summary>
/// Request DTO for reject review endpoint
/// </summary>
public class RejectReviewRequest
{
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for reply to review endpoint
/// </summary>
public class ReplyToReviewRequest
{
    public string Reply { get; set; } = string.Empty;
}