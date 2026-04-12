namespace Presentation.Media.Requests;

public record GetAllMediaRequest(
    string? EntityType = null,
    int Page = 1,
    int PageSize = 20);

public record SetPrimaryMediaRequest(Guid MediaId);

public record ReorderMediaRequest(
    string EntityType,
    int EntityId,
    ICollection<int> OrderedMediaIds);