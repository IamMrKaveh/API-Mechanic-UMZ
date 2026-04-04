namespace MainApi.Media.Requests;

public record SetPrimaryMediaRequest(
    int MediaId,
    string EntityType,
    int EntityId);

public record ReorderMediaRequest(
    string EntityType,
    int EntityId,
    IReadOnlyList<int> OrderedMediaIds
);