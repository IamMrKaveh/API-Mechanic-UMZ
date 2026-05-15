using Application.Media.Features.Queries.GetEntityMedia;
using Application.Media.Features.Queries.GetMediaById;
using Application.Media.Features.Shared;

namespace Presentation.Media.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class MediaController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("{entityType}/{entityId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MediaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<MediaDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMediaForEntity(
        string entityType,
        Guid entityId,
        CancellationToken ct)
    {
        var query = new GetEntityMediaQuery(entityType, entityId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMediaById(Guid id, CancellationToken ct)
    {
        var query = new GetMediaByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}