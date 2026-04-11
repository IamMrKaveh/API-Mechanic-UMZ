using Domain.Media.ValueObjects;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(IMediaService mediaService) : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);
        return mediaService.DeleteAsync(mediaId, ct);
    }
}