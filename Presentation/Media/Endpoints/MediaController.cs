using Application.Media.Features.Queries.GetEntityMedia;
using Application.Media.Features.Queries.GetMediaById;

namespace Presentation.Media.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class MediaController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{entityType}/{entityId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMediaForEntity(string entityType, int entityId)
    {
        var query = new GetEntityMediaQuery(entityType, entityId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMediaById(Guid id)
    {
        var query = new GetMediaByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}