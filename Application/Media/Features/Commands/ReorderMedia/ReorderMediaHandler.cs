namespace Application.Media.Features.Commands.ReorderMedia;

public class ReorderMediaHandler(IMediaService mediaService) : IRequestHandler<ReorderMediaCommand, ServiceResult>
{
    public Task<ServiceResult> Handle(ReorderMediaCommand request, CancellationToken ct)
        => mediaService.ReorderAsync(request.EntityType, request.EntityId, request.OrderedIds, ct);
}