using Application.Media.Features.Commands.CleanupOrphanedMedia;
using Application.Media.Features.Commands.DeleteMedia;
using Application.Media.Features.Commands.ReorderMedia;
using Application.Media.Features.Commands.SetPrimaryMedia;
using Application.Media.Features.Commands.UploadMedia;
using Application.Media.Features.Queries.GetAllMedia;
using Presentation.Base.Controllers.v1;
using Presentation.Media.Requests;

namespace Presentation.Media.Controllers;

[Route("api/admin/media")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminMediaController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAllMedia(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? entityType = null)
    {
        var query = new GetAllMediaQuery(entityType, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("cleanup-orphaned")]
    public async Task<IActionResult> CleanupOrphaned()
    {
        var command = new CleanupOrphanedMediaCommand();
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string entityType,
        [FromForm] int entityId,
        [FromForm] bool isPrimary = false,
        [FromForm] string? altText = null)
    {
        var command = new UploadMediaCommand
        {
            FileStream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            EntityType = entityType,
            EntityId = entityId,
            IsPrimary = isPrimary,
            AltText = altText
        };

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{mediaId}")]
    public async Task<IActionResult> DeleteMedia(int mediaId)
    {
        var command = new DeleteMediaCommand(mediaId, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("set-primary")]
    public async Task<IActionResult> SetPrimaryMedia([FromBody] SetPrimaryMediaRequest request)
    {
        var command = new SetPrimaryMediaCommand(request.MediaId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderMedia([FromBody] ReorderMediaRequest request)
    {
        var command = new ReorderMediaCommand
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            OrderedMediaIds = request.OrderedMediaIds
        };
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}