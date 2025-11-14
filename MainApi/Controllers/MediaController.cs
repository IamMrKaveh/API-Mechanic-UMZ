using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IMediaService mediaService, ILogger<MediaController> logger)
    {
        _mediaService = mediaService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(10_485_760)] // 10 MB
    public async Task<ActionResult<MediaDto>> UploadMedia([FromForm] List<IFormFile> files, [FromForm] string entityType, [FromForm] int entityId, [FromForm] bool isPrimary = false, [FromForm] string? altText = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

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


    [HttpGet("{entityType}/{entityId}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MediaDto>>> GetMediaForEntity(string entityType, int entityId)
    {
        var media = await _mediaService.GetMediaForEntityAsync(entityType, entityId);
        return Ok(media);
    }

    [HttpDelete("{mediaId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMedia(int mediaId)
    {
        var success = await _mediaService.DeleteMediaAsync(mediaId);
        if (!success)
        {
            return NotFound("Media not found.");
        }
        return NoContent();
    }

    [HttpPatch("set-primary")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SetPrimaryMedia([FromBody] SetPrimaryMediaRequestDto request)
    {
        var success = await _mediaService.SetPrimaryMediaAsync(request.MediaId, request.EntityId, request.EntityType);
        if (!success)
        {
            return NotFound("Media or entity not found.");
        }
        return NoContent();
    }
}

public class SetPrimaryMediaRequestDto
{
    public int MediaId { get; set; }
    public int EntityId { get; set; }
    public required string EntityType { get; set; }
}