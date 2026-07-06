using System.ComponentModel.DataAnnotations;

namespace Application.Auth.Features.Shared;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    [MinLength(32, ErrorMessage = "JWT Key باید حداقل ۳۲ کاراکتر باشد.")]
    public string Key { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int AccessTokenExpirationMinutes { get; init; } = 60;

    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; init; } = 30;
}