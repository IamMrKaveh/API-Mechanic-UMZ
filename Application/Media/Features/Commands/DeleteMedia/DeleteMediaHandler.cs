using Application.Common.Results;
using Application.Media.Contracts;

namespace Application.Media.Features.Commands.DeleteMedia;

public class DeleteMediaHandler(IMediaService mediaService) : IRequestHandler<DeleteMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(DeleteMediaCommand request, CancellationToken ct)
        => mediaService.DeleteAsync(request.MediaId, ct);
}