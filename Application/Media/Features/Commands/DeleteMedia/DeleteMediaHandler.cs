using Domain.Media.ValueObjects;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(IMediaService mediaService) : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(DeleteMediaCommand request, CancellationToken ct)
        => mediaService.DeleteAsync(MediaId.From(request.MediaId), ct);
}