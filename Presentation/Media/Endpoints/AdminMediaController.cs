using Application.Media.Features.Commands.CleanupOrphanedMedia;
using Application.Media.Features.Commands.DeleteMedia;
using Application.Media.Features.Commands.ReorderMedia;
using Application.Media.Features.Commands.SetPrimaryMedia;
using Application.Media.Features.Commands.UploadMedia;
using Application.Media.Features.Queries.GetAllMedia;
using MapsterMapper;
using Presentation.Media.Requests;

namespace Presentation.Media.Endpoints;

[Route("api/admin/media")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminMediaController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetAllMedia(
        [FromQuery] GetAllMediaRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAllMediaQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("cleanup-orphaned")]
    public async Task<IActionResult> CleanupOrphaned(CancellationToken ct)
    {
        var result = await Mediator.Send(new CleanupOrphanedMediaCommand(), ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] IFormFile file,
        [FromForm] string entityType,
        [FromForm] Guid entityId,
        [FromForm] bool isPrimary = false,
        [FromForm] string? altText = null,
        CancellationToken ct = default)
    {
        var command = new UploadMediaCommand(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length,
            entityType,
            entityId,
            isPrimary,
            altText);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{mediaId:guid}")]
    public async Task<IActionResult> DeleteMedia(Guid mediaId, CancellationToken ct)
    {
        var command = new DeleteMediaCommand(mediaId, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("set-primary")]
    public async Task<IActionResult> SetPrimaryMedia(
        [FromBody] SetPrimaryMediaRequest request,
        CancellationToken ct)
    {
        var command = new SetPrimaryMediaCommand(request.MediaId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderMedia(
        [FromBody] ReorderMediaRequest request,
        CancellationToken ct)
    {
        var command = new ReorderMediaCommand(
            request.EntityType,
            request.EntityId,
            request.OrderedMediaIds);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}