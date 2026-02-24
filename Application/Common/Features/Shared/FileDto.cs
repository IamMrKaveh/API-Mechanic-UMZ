namespace Application.Common.Features.Shared;

public class FileDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Length { get; init; }
    public Stream Content { get; init; } = new MemoryStream();
}