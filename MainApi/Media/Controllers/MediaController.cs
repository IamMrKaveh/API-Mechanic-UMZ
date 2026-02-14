namespace MainApi.Media.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MediaController : BaseApiController
{
    private readonly IMediator _mediator;

    public MediaController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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
    public async Task<IActionResult> GetMediaById(int id)
    {
        var query = new GetMediaByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}