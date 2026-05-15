using Application.Media.Features.Commands.CleanupOrphanedMedia;
using Application.Media.Features.Commands.DeleteMedia;
using Application.Media.Features.Commands.ReorderMedia;
using Application.Media.Features.Commands.SetPrimaryMedia;
using Application.Media.Features.Commands.UploadMedia;
using Application.Media.Features.Queries.GetAllMedia;
using Application.Media.Features.Shared;
using Presentation.Media.Requests;

namespace Presentation.Media.Endpoints;

[Route("api/admin/media")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminMediaController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MediaDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MediaDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllMedia(
        [FromQuery] GetAllMediaRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAllMediaQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("cleanup-orphaned")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CleanupOrphaned(CancellationToken ct)
    {
        var command = new CleanupOrphanedMediaCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<MediaDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UploadMedia(
        [FromForm] UploadMediaRequest request,
        CancellationToken ct)
    {
        var command = new UploadMediaCommand(
            request.File.OpenReadStream(),
            request.File.FileName,
            request.File.ContentType,
            request.File.Length,
            request.EntityType,
            request.EntityId,
            request.IsPrimary,
            request.AltText);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{mediaId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteMedia(Guid mediaId, CancellationToken ct)
    {
        var command = new DeleteMediaCommand(mediaId, CurrentUser.UserId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("set-primary")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetPrimaryMedia(
        [FromBody] SetPrimaryMediaRequest request,
        CancellationToken ct)
    {
        var command = new SetPrimaryMediaCommand(request.MediaId);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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