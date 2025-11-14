namespace Application.DTOs;

public class ZarinpalSettingsDto
{
    public required bool IsSandbox { get; init; }
    public required string MerchantId { get; init; }
}

public class FrontendUrlsDto
{
    public required string BaseUrl { get; init; }
}