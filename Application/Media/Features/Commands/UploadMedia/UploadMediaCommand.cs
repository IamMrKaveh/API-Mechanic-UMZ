namespace Application.Media.Features.Commands.UploadMedia;

public record UploadMediaCommand : IRequest<ServiceResult<MediaDto>>
{
    public required Stream FileStream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long FileSize { get; init; }
    public required string EntityType { get; init; }
    public required int EntityId { get; init; }
    public bool IsPrimary { get; init; }
    public string? AltText { get; init; }
}