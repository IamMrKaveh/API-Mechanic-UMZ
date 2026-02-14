namespace Application.Media.Features.Commands.ReorderMedia;

public record ReorderMediaCommand : IRequest<ServiceResult>
{
    public required string EntityType { get; init; }
    public required int EntityId { get; init; }
    public required IReadOnlyList<int> OrderedMediaIds { get; init; }
}