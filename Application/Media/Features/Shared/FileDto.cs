namespace Application.Media.Features.Shared;

public record FileDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Size { get; init; }
    public Stream Stream { get; init; } = Stream.Null;
}