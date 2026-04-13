using Application.Media.Features.Shared;
using Domain.Media.Services;

namespace Application.Media.Features.Commands.UploadMedia;

public class UploadMediaHandler(
    IMediaService mediaService) : IRequestHandler<UploadMediaCommand, ServiceResult<MediaDto>>
{
    public Task<ServiceResult<MediaDto>> Handle(UploadMediaCommand request, CancellationToken ct)
    {
        var filePath = FilePath.CreateForUpload(request.EntityType, request.FileName);
        var fileSize = FileSize.Create(request.FileSize);

        var validationResult = MediaDomainService.ValidateFileTypeForEntity(request.EntityType, filePath);
        if (validationResult.IsFailure)
        {
            return Task.FromResult(ServiceResult<MediaDto>.Validation(validationResult.Error.Message));
        }

        return mediaService.UploadAsync(
            request.FileStream,
            filePath,
            fileSize,
            request.EntityType,
            request.EntityId,
            request.IsPrimary,
            request.AltText,
            ct);
    }
}