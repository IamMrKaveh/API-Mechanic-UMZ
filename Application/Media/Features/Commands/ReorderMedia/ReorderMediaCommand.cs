namespace Application.Media.Features.Commands.ReorderMedia;

public record ReorderMediaCommand(
    string EntityType,
    int EntityId,
    ICollection<Guid> OrderedIds) : IRequest<ServiceResult>;