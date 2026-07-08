namespace Application.Common.Options;

public sealed class ApiBaseUrlOptions
{
    public const string SectionName = "Api";

    public string PublicBaseUrl { get; set; } = "https://localhost:44318";
}