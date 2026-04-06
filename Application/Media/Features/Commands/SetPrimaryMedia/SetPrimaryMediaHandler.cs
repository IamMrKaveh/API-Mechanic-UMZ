using Application.Common.Results;
using Application.Media.Contracts;

namespace Application.Media.Features.Commands.SetPrimaryMedia;

public class SetPrimaryMediaHandler(IMediaService mediaService) : IRequestHandler<SetPrimaryMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(SetPrimaryMediaCommand request, CancellationToken ct)
        => mediaService.SetAsPrimaryAsync(request.MediaId, ct);
}