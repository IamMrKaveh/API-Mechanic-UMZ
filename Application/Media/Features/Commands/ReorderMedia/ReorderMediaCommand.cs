namespace Application.Media.Features.Commands.ReorderMedia;

public record ReorderMediaCommand(
    string EntityType,
    Guid EntityId,
    IReadOnlyCollection<Guid> OrderedIds) : IRequest<ServiceResult>;