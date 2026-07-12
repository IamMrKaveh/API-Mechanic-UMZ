using Application.Review.Features.Commands.CreateReview;
using Application.Review.Features.Commands.DeleteOwnReview;
using Application.Review.Features.Commands.UpdateOwnReview;
using Application.Review.Features.Queries.CanReviewProduct;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetProductReviewSummary;
using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/v{version:apiVersion}/reviews")]
[ApiController]
public class ReviewsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("products/{productId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(ApiResponse<ReviewSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        Guid productId,
        CancellationToken ct)
    {
        return await Send(new GetProductReviewSummaryQuery(productId), ct);
    }

    [HttpGet("products/{productId:guid}/can-review")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<CanReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CanReview(
        Guid productId,
        CancellationToken ct)
    {
        return await Send(new CanReviewProductQuery(productId), ct);
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ProductReviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        return await Send(Mapper.Map<CreateReviewCommand>(request), ct);
    }

    [HttpPut("{reviewId:guid}")]
    [Authorize]
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

    [HttpDelete("{reviewId:guid}/me")]
    [Authorize]
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
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductReviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        return await Send(new GetUserReviewsQuery(RequestContext.UserId ?? Guid.Empty, page, pageSize), ct);
    }
}