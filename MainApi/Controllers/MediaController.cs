namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet("{entityType}/{entityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MediaDto>>> GetMediaForEntity(string entityType, int entityId)
    {
        var media = await _mediaService.GetMediaForEntityAsync(entityType, entityId);
        return Ok(media);
    }
}

public class SetPrimaryMediaRequestDto
{
    public int MediaId { get; set; }
    public int EntityId { get; set; }
    public required string EntityType { get; set; }
}