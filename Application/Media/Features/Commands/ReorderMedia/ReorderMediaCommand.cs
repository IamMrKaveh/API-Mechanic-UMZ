using Application.Common.Results;

namespace Application.Media.Features.Commands.ReorderMedia;

public record ReorderMediaCommand(
    string EntityType,
    int EntityId,
    List<Guid> OrderedIds) : IRequest<ServiceResult>;