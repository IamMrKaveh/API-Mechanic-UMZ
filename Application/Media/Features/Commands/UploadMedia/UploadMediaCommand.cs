using Application.Media.Features.Shared;

namespace Application.Media.Features.Commands.UploadMedia;

public record UploadMediaCommand(
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSize,
    string EntityType,
    int EntityId,
    bool IsPrimary = false,
    string? AltText = null) : IRequest<ServiceResult<MediaDto>>;