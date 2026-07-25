using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Application.Review.Features.Commands.ReplyToReview;
using Application.Review.Features.Commands.UpdateReviewStatus;
using Application.Review.Features.Queries.GetReviewById;
using Application.Review.Features.Queries.GetReviewsByStatus;
using Application.Review.Features.Shared;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/v{version:apiVersion}/admin/reviews")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminReviewsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    private static readonly HashSet<string> AllowedStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Pending", "Approved", "Rejected", "All" };

    [HttpGet]
    [SwaggerOperation(OperationId = "AdminReviews_List")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var normalized = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        if (!AllowedStatuses.Contains(normalized))
            return BadRequest("پارامتر status نامعتبر است. مقادیر مجاز: Pending، Approved، Rejected، All.");

        var canonical = AllowedStatuses.First(s => s.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        return await Send(new GetReviewsByStatusQuery(canonical, page, pageSize), ct);
    }

    [HttpGet("{reviewId:guid}")]
    [SwaggerOperation(OperationId = "AdminReviews_GetById")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid reviewId, CancellationToken ct)
    {
        return await Send(new GetReviewByIdQuery(reviewId), ct);
    }

    [HttpPost("{reviewId:guid}/reply")]
    [SwaggerOperation(OperationId = "AdminReviews_Reply")]
    public async Task<IActionResult> ReplyToReview(Guid reviewId, [FromBody] ReplyToReviewRequest request, CancellationToken ct)
        => await Send(new ReplyToReviewCommand(reviewId, request.Reply), ct);

    [HttpDelete("{reviewId:guid}")]
    [SwaggerOperation(OperationId = "AdminReviews_Delete")]
    public async Task<IActionResult> DeleteReview(Guid reviewId, [FromBody] DeleteReviewRequest request, CancellationToken ct)
    {
        var command = Mapper.Map<DeleteReviewCommand>(request).Enrich(reviewId);
        return await Send(command, ct);
    }

    [HttpPatch("{reviewId:guid}/approve")]
    [SwaggerOperation(OperationId = "AdminReviews_Approve")]
    public async Task<IActionResult> ApproveReview(Guid reviewId, [FromBody] ApproveReviewRequest request, CancellationToken ct)
    {
        var command = Mapper.Map<ApproveReviewCommand>(request).Enrich(reviewId);
        return await Send(command, ct);
    }

    [HttpPatch("{reviewId:guid}/reject")]
    [SwaggerOperation(OperationId = "AdminReviews_Reject")]
    public async Task<IActionResult> RejectReview(Guid reviewId, [FromBody] RejectReviewRequest request, CancellationToken ct)
    {
        var command = Mapper.Map<RejectReviewCommand>(request).Enrich(reviewId);
        return await Send(command, ct);
    }

    [HttpPatch("{reviewId:guid}/status")]
    [SwaggerOperation(OperationId = "AdminReviews_UpdateStatus")]
    public async Task<IActionResult> UpdateReviewStatus(Guid reviewId, [FromBody] UpdateReviewStatusRequest request, CancellationToken ct)
        => await Send(new UpdateReviewStatusCommand(reviewId, request.Status), ct);
}
