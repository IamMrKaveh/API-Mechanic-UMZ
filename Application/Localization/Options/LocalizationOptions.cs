namespace Application.Localization.Options;

public sealed class LocalizationOptions
{
    public const string SectionName = "Localization";

    public string DefaultCulture { get; init; } = "fa-IR";

    public string[] SupportedCultures { get; init; } = ["fa-IR", "en-US"];

    public string ResourcesPath { get; init; } = "Resources";

    public bool FallbackToParentCultures { get; init; } = true;
}
