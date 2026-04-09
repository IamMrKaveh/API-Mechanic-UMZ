using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Application.Review.Features.Commands.ReplyToReview;
using Application.Review.Features.Commands.UpdateReviewStatus;
using Application.Review.Features.Queries.GetReviewsByStatus;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetReviewsByStatusQuery(status, page, pageSize));
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(Guid reviewId)
    {
        var result = await _mediator.Send(new ApproveReviewCommand(reviewId));
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(
        Guid reviewId,
        [FromBody] RejectReviewRequest request)
    {
        var result = await _mediator.Send(new RejectReviewCommand(reviewId, request.Reason));
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId}/status")]
    public async Task<IActionResult> UpdateReviewStatus(
        Guid reviewId,
        [FromBody] UpdateReviewStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateReviewStatusCommand(reviewId, request.Status));
        return ToActionResult(result);
    }

    [HttpPost("{reviewId}/reply")]
    public async Task<IActionResult> ReplyToReview(
        Guid reviewId,
        [FromBody] ReplyToReviewRequest request)
    {
        var result = await _mediator.Send(new ReplyToReviewCommand(reviewId, request.Reply));
        return ToActionResult(result);
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(Guid reviewId)
    {
        var result = await _mediator.Send(new DeleteReviewCommand(reviewId));
        return ToActionResult(result);
    }
}