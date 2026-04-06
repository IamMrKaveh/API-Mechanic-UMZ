using Application.Common.Results;
using Application.Media.Contracts;
using Application.Media.Features.Shared;

namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaHandler(
    IMediaService mediaService) : IRequestHandler<UploadMediaCommand, ServiceResult<MediaDto>>
{
    public Task<ServiceResult<MediaDto>> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        return mediaService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.FileSize,
            request.EntityType,
            request.EntityId,
            request.IsPrimary,
            request.AltText,
            ct);
    }
}