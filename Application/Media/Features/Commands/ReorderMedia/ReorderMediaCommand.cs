namespace Application.Media.Features.Commands.ReorderMedia;

public record ReorderMediaCommand(
    string EntityType,
    Guid EntityId,
    ICollection<Guid> OrderedIds) : IRequest<ServiceResult>;