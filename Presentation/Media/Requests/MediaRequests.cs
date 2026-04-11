namespace Presentation.Media.Requests;

public record SetPrimaryMediaRequest(Guid MediaId);

public record ReorderMediaRequest(
    string EntityType,
    int EntityId,
    ICollection<int> OrderedMediaIds
);