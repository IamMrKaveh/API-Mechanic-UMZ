namespace Application.DTOs.Media;

public class MediaDto
{
    public int Id { get; set; }
    public string? Url { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}