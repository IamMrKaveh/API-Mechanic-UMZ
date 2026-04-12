using Application.Review.Features.Commands.CreateReview;
using Application.Review.Features.Queries.GetProductReviews;
using Application.Review.Features.Queries.GetUserReviews;
using MapsterMapper;
using Presentation.Review.Mapping;
using Presentation.Review.Requests;

namespace Presentation.Review.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class ReviewsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("product/{productId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetReviews(
        Guid productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var query = new GetProductReviewsQuery(productId, page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var command = Mapper
            .Map<CreateReviewCommand>(request)
            .Enrich(CurrentUser.UserId);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var result = await Mediator.Send(
            new GetUserReviewsQuery(CurrentUser.UserId, page, pageSize), ct);

        return ToActionResult(result);
    }
}