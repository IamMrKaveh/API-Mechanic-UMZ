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
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetReviewsByStatusQuery(status, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("{reviewId:guid}/reply")]
    public async Task<IActionResult> ReplyToReview(
        Guid reviewId,
        [FromBody] ReplyToReviewRequest request,
        CancellationToken ct)
    {
        var command = new ReplyToReviewCommand(reviewId, request.Reply, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{reviewId:guid}")]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromBody] DeleteReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper
            .Map<DeleteReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId:guid}/approve")]
    public async Task<IActionResult> ApproveReview(
        Guid reviewId,
        [FromBody] ApproveReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper
            .Map<ApproveReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId:guid}/reject")]
    public async Task<IActionResult> RejectReview(
        Guid reviewId,
        [FromBody] RejectReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper
            .Map<RejectReviewCommand>(request)
            .Enrich(reviewId, CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{reviewId:guid}/status")]
    public async Task<IActionResult> UpdateReviewStatus(
        Guid reviewId,
        [FromBody] UpdateReviewStatusRequest request,
        CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateReviewStatusCommand(reviewId, request.Status), ct);
        return ToActionResult(result);
    }
}