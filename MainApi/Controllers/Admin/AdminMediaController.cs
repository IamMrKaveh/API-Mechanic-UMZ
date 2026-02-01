using Application.Common.Interfaces.Admin.Media;
using Application.Common.Interfaces.Media;
using Application.DTOs.Media;
using MainApi.Controllers.Media;

namespace MainApi.Controllers.Admin;

[Route("api/admin/media")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminMediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly IAdminMediaService _adminMediaService;
    private readonly ILogger<AdminMediaController> _logger;

    public AdminMediaController(IMediaService mediaService, IAdminMediaService adminMediaService, ILogger<AdminMediaController> logger)
    {
        _mediaService = mediaService;
        _adminMediaService = adminMediaService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllMedia([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? entityType = null)
    {
        var result = await _adminMediaService.GetAllMediaAsync(page, pageSize, entityType);
        if (!result.Success) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpPost("cleanup-orphaned")]
    public async Task<IActionResult> CleanupOrphaned()
    {
        var result = await _adminMediaService.CleanupOrphanedMediaAsync();
        if (!result.Success) return BadRequest(result.Error);
        return Ok(result.Data);
    }

    [HttpPost]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<ActionResult<MediaDto>> UploadMedia([FromForm] List<IFormFile> files, [FromForm] string entityType, [FromForm] int entityId, [FromForm] bool isPrimary = false, [FromForm] string? altText = null)
    {
        if (files == null || files.Count == 0) return BadRequest("No files uploaded.");

        try
        {
            var fileStreams = files.Select(file => (file.OpenReadStream(), file.FileName, file.ContentType, file.Length));
            var uploadedMedia = await _mediaService.UploadFilesAsync(fileStreams, entityType, entityId, isPrimary, altText);
            return Ok(uploadedMedia);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media for {EntityType} {EntityId}", entityType, entityId);
            return StatusCode(500, "An error occurred while uploading media.");
        }
    }

    [HttpDelete("{mediaId}")]
    public async Task<IActionResult> DeleteMedia(int mediaId)
    {
        var success = await _mediaService.DeleteMediaAsync(mediaId);
        if (!success) return NotFound("Media not found.");
        return NoContent();
    }

    [HttpPatch("set-primary")]
    public async Task<IActionResult> SetPrimaryMedia([FromBody] SetPrimaryMediaRequestDto request)
    {
        var success = await _mediaService.SetPrimaryMediaAsync(request.MediaId, request.EntityId, request.EntityType);
        if (!success) return NotFound("Media or entity not found.");
        return NoContent();
    }
}