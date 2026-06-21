namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaHandler(
    IMediaService mediaService)
    : ICommandHandler<ReorderMediaCommand>
{
    public Task<ServiceResult> Handle(ReorderMediaCommand request, CancellationToken ct)
    {
        return mediaService.ReorderAsync(request.EntityType, request.EntityId, request.OrderedIds, ct);
    }
}