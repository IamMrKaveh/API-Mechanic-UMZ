namespace Application.Common.Features.Shared;

public class FileDto
{
    public string FileName { get; init; }
    public string ContentType { get; init; }
    public long Length { get; init; }
    public Stream Content { get; init; }
}