using Application.Review.Features.Commands.CreateReview;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetUserReviews;
using Application.Review.Features.Shared;
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
        CancellationToken ct = default)
    {
        return await Send(new GetProductReviewsQuery(productId, page, pageSize), ct);
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