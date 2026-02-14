namespace Application.Common.Features.Shared;

public class FileDto
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Length { get; set; }
    public Stream Content { get; set; }
}