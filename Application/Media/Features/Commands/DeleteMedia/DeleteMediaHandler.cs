using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(
    IMediaService mediaService,
    ICurrentUserService currentUserService) : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);
        var userId = UserId.From(currentUserService.UserId.Value);

        return mediaService.DeleteAsync(mediaId, ct);
    }
}