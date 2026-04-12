using Application.Media.Features.Queries.GetEntityMedia;
using Application.Media.Features.Queries.GetMediaById;
using MapsterMapper;

namespace Presentation.Media.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class MediaController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("{entityType}/{entityId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMediaForEntity(
        string entityType,
        int entityId,
        CancellationToken ct)
    {
        var query = new GetEntityMediaQuery(entityType, entityId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMediaById(Guid id, CancellationToken ct)
    {
        var query = new GetMediaByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}