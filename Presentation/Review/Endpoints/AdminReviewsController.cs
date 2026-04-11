using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Application.Review.Features.Commands.ReplyToReview;
using Application.Review.Features.Commands.UpdateReviewStatus;
using Application.Review.Features.Queries.GetReviewsByStatus;
using MapsterMapper;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "open",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetReviewsByStatusQuery(
            status,
            page,
            pageSize);

        var result = await Mediator.Send(query);

        return ToActionResult(result);
    }

    [HttpPost("{reviewId}/reply")]
    public async Task<IActionResult> ReplyToReview(
        Guid reviewId,
        [FromBody] ReplyToReviewRequest request)
    {
        var command = new ReplyToReviewCommand(
            reviewId,
            request.Reply,
            CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{reviewId}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromBody] DeleteReviewRequest request)
    {
        var command = Mapper
            .Map<DeleteReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{reviewId}/approve")]
    public async Task<IActionResult> ApproveReview(
        Guid reviewId,
        [FromBody] ApproveReviewRequest request)
    {
        var command = Mapper
            .Map<ApproveReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command);

        return ToActionResult(result);
    }

    [HttpPatch("{reviewId}/reject")]
    public async Task<IActionResult> RejectReview(
        Guid reviewId,
        [FromBody] RejectReviewRequest request)
    {
        var command = Mapper
            .Map<RejectReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command);

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
}