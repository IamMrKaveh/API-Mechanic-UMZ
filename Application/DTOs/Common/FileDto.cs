namespace Application.DTOs.Common;

public class FileDto
{
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long Length { get; set; }
    public required Stream Content { get; set; }
}