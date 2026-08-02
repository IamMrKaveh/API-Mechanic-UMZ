using Application.Review.Features.Commands.ApproveReview;
using Application.Review.Features.Commands.BulkOperation;
using Application.Review.Features.Commands.DeleteReview;
using Application.Review.Features.Commands.RejectReview;
using Application.Review.Features.Commands.RemoveAdminReply;
using Application.Review.Features.Commands.ReplyToReview;
using Application.Review.Features.Commands.RestoreReview;
using Application.Review.Features.Commands.UpdateAdminReply;
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
public class AdminReviewsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_List")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetReviewsByStatus(
        [FromQuery] string status = "Pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetReviewsByStatusQuery(status, page, pageSize), ct);
    }

    [HttpGet("{reviewId:guid}")]
    [SwaggerOperation(OperationId = "AdminReviews_GetById")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid reviewId, CancellationToken ct)
    {
        return await Send(new GetReviewByIdQuery(reviewId), ct);
    }

    [HttpPatch("{reviewId:guid}/approve")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_Approve")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReview(Guid reviewId, CancellationToken ct)
        => await Send(new ApproveReviewCommand(reviewId), ct);

    [HttpPatch("{reviewId:guid}/reject")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_Reject")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectReview(
        Guid reviewId,
        [FromBody] RejectReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<RejectReviewCommand>(request).Enrich(reviewId);
        return await Send(command, ct);
    }

    [HttpPatch("{reviewId:guid}/status")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_UpdateStatus")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReviewStatus(
        Guid reviewId,
        [FromBody] UpdateReviewStatusRequest request,
        CancellationToken ct)
        => await Send(new UpdateReviewStatusCommand(reviewId, request.Status, request.Reason), ct);

    [HttpDelete("{reviewId:guid}")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_Delete")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(
        Guid reviewId,
        [FromQuery] string? reason,
        CancellationToken ct)
        => await Send(new DeleteReviewCommand(reviewId, reason), ct);

    [HttpPost("{reviewId:guid}/restore")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_Restore")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreReview(Guid reviewId, CancellationToken ct)
        => await Send(new RestoreReviewCommand(reviewId), ct);

    [HttpPost("{reviewId:guid}/reply")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_Reply")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplyToReview(
        Guid reviewId,
        [FromBody] ReplyToReviewRequest request,
        CancellationToken ct)
        => await Send(new ReplyToReviewCommand(reviewId, request.Reply), ct);

    [HttpPut("{reviewId:guid}/reply")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_UpdateReply")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateReply(
        Guid reviewId,
        [FromBody] ReplyToReviewRequest request,
        CancellationToken ct)
        => await Send(new UpdateAdminReplyCommand(reviewId, request.Reply), ct);

    [HttpDelete("{reviewId:guid}/reply")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_RemoveReply")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReply(Guid reviewId, CancellationToken ct)
        => await Send(new RemoveAdminReplyCommand(reviewId), ct);

    [HttpPost("bulk/approve")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_BulkApprove")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkApprove(
        [FromBody] BulkReviewActionRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<BulkApproveReviewsCommand>(request);
        return await Send(command, ct);
    }

    [HttpPost("bulk/reject")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_BulkReject")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkReject(
        [FromBody] BulkRejectReviewsRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<BulkRejectReviewsCommand>(request);
        return await Send(command, ct);
    }

    [HttpPost("bulk/delete")]
    [ReviewRateLimit(ReviewRateLimitPolicy.AdminAction)]
    [SwaggerOperation(OperationId = "AdminReviews_BulkDelete")]
    [ProducesResponseType(typeof(ApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkDelete(
        [FromBody] BulkDeleteReviewsRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<BulkDeleteReviewsCommand>(request);
        return await Send(command, ct);
    }
}
