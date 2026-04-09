namespace Presentation.Media.Requests;

public record SetPrimaryMediaRequest(Guid MediaId);

public record ReorderMediaRequest(
    string EntityType,
    Guid EntityId,
    ICollection<Guid> OrderedMediaIds
);