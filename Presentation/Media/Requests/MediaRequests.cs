namespace Presentation.Media.Requests;

public record GetAllMediaRequest(
    string? EntityType = null,
    int Page = 1,
    int PageSize = 20);

public record SetPrimaryMediaRequest(Guid MediaId);

public record ReorderMediaRequest(
    string EntityType,
    Guid EntityId,
    ICollection<Guid> OrderedMediaIds);

public sealed class UploadMediaRequest
{
    public IFormFile File { get; init; } = default!;
    public string EntityType { get; init; } = string.Empty;
    public Guid EntityId { get; init; }
    public bool IsPrimary { get; init; }
    public string? AltText { get; init; }
}