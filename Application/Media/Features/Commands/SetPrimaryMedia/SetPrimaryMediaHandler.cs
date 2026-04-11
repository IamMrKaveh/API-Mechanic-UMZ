using Domain.Media.ValueObjects;

namespace Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandler(IMediaService mediaService) : IRequestHandler<SetPrimaryMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(SetPrimaryMediaCommand request, CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);
        return mediaService.SetAsPrimaryAsync(mediaId, ct);
    }
}