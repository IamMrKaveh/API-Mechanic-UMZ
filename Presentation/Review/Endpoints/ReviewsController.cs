using Application.Review.Features.Commands.CreateReview;
using Application.Review.Features.Commands.DeleteOwnReview;
using Application.Review.Features.Commands.DislikeReview;
using Application.Review.Features.Commands.LikeReview;
using Application.Review.Features.Commands.RemoveReviewVote;
using Application.Review.Features.Commands.UpdateOwnReview;
using Application.Review.Features.Queries.CanReviewProduct;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetProductReviewSummary;
using Application.Review.Features.Queries.GetReviewById;
using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/v{version:apiVersion}/reviews")]
[ApiController]
public class ReviewsController(IMediator mediator, IMapper mapper)
    : BaseApiController(mediator, mapper)
{
    [HttpGet("products/{productId:guid}")]
    [AllowAnonymous]
    [ReviewRateLimit(ReviewRateLimitPolicy.PublicRead)]
    [SwaggerOperation(OperationId = "Reviews_GetForProduct")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sortBy = "Newest",
        [FromQuery] int? minRating = null,
        [FromQuery] bool verifiedOnly = false,
        CancellationToken ct = default)
    {
        return await Send(
            new GetProductReviewsQuery(productId, page, pageSize, sortBy, minRating, verifiedOnly),
            ct);
    }

    [HttpGet("products/{productId:guid}/summary")]
    [AllowAnonymous]
    [ReviewRateLimit(ReviewRateLimitPolicy.PublicRead)]
    [SwaggerOperation(OperationId = "Reviews_GetSummary")]
    [ProducesResponseType(typeof(ApiResponse<ReviewSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        Guid productId,
        CancellationToken ct)
    {
        return await Send(new GetProductReviewSummaryQuery(productId), ct);
    }

    [HttpGet("products/{productId:guid}/can-review")]
    [Authorize]
    [SwaggerOperation(OperationId = "Reviews_CanReview")]
    [ProducesResponseType(typeof(ApiResponse<CanReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanReview(
        Guid productId,
        [FromQuery] Guid? orderId,
        CancellationToken ct)
    {
        return await Send(new CanReviewProductQuery(productId, orderId), ct);
    }

    [HttpPost]
    [Authorize]
    [ReviewRateLimit(ReviewRateLimitPolicy.CreateReview)]
    [SwaggerOperation(OperationId = "Reviews_Create")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<CreateReviewCommand>(request);
        return await SendCreated(command, ct);
    }

    [HttpGet("{reviewId:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(OperationId = "Reviews_GetById")]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid reviewId, CancellationToken ct)
    {
        return await Send(new GetReviewByIdQuery(reviewId), ct);
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize]
    [ReviewRateLimit(ReviewRateLimitPolicy.CreateReview)]
    [SwaggerOperation(OperationId = "Reviews_UpdateOwn")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOwn(
        Guid reviewId,
        [FromBody] UpdateOwnReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper.Map<UpdateOwnReviewCommand>(request).Enrich(reviewId);
        return await Send(command, ct);
    }

    [HttpDelete("me/{reviewId:guid}")]
    [Authorize]
    [SwaggerOperation(OperationId = "Reviews_DeleteOwn")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOwn(
        Guid reviewId,
        CancellationToken ct)
    {
        return await Send(new DeleteOwnReviewCommand(reviewId), ct);
    }

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(OperationId = "Reviews_GetMine")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetUserReviewsQuery(page, pageSize), ct);
    }

    [HttpPost("{reviewId:guid}/like")]
    [Authorize]
    [ReviewRateLimit(ReviewRateLimitPolicy.Vote)]
    [SwaggerOperation(OperationId = "Reviews_Like")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LikeReview(
        Guid reviewId,
        CancellationToken ct)
        => await Send(new LikeReviewCommand(reviewId), ct);

    [HttpPost("{reviewId:guid}/dislike")]
    [Authorize]
    [ReviewRateLimit(ReviewRateLimitPolicy.Vote)]
    [SwaggerOperation(OperationId = "Reviews_Dislike")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DislikeReview(
        Guid reviewId,
        CancellationToken ct)
        => await Send(new DislikeReviewCommand(reviewId), ct);

    [HttpDelete("{reviewId:guid}/vote")]
    [Authorize]
    [ReviewRateLimit(ReviewRateLimitPolicy.Vote)]
    [SwaggerOperation(OperationId = "Reviews_RemoveVote")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RemoveReviewVote(
        Guid reviewId,
        CancellationToken ct)
        => await Send(new RemoveReviewVoteCommand(reviewId), ct);
}
