using Application.Common.Interfaces;
using Application.Media.Contracts;
using Domain.Media.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(
    IMediaService mediaService,
    ICurrentUserService currentUserService)
    : ICommandHandler<DeleteMediaCommand>
{
    public Task<ServiceResult> Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var mediaId = MediaId.From(request.MediaId);

        UserId? deletedBy = currentUserService.UserId.HasValue
            ? UserId.From(currentUserService.UserId.Value)
            : null;

        return mediaService.DeleteAsync(mediaId, deletedBy, ct);
    }
}